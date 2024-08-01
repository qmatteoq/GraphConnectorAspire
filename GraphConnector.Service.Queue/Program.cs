using Azure.Core.Pipeline;
using Azure.Identity;
using GraphConnector.Library.Configuration;
using GraphConnector.Library.Connection;
using GraphConnector.Service.Queue;
using Microsoft.Graph;
using Microsoft.Identity.Client;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddRabbitMQClient("messaging");
//builder.Configuration.AddUserSecrets("dotnet-GraphConnector.Service.Queue-c351d448-6acf-4794-ae62-71d9f5c336d1");
builder.Services.AddSingleton(s =>
{
    var handler = Utils.GetHttpClientHandler();
    var options = new ClientSecretCredentialOptions
    {
        Transport = new HttpClientTransport(handler)
    };

    var config = builder.Configuration;

    var clientId = config["Entra:ClientId"];
    var clientSecret = config["Entra:ClientSecret"];
    var tenantId = config["Entra:TenantId"];

    var credential = new ClientSecretCredential(tenantId, clientId, clientSecret, options);
    var handlers = GraphClientFactory.CreateDefaultHandlers();

    var proxy = Utils.GetWebProxy();
    var httpClient = GraphClientFactory.Create(handlers, proxy: proxy);

    return new GraphServiceClient(httpClient, credential);
});

builder.Services.AddSingleton(s =>
{
    var config = builder.Configuration;

    var clientId = config["Entra:ClientId"];
    var clientSecret = config["Entra:ClientSecret"];
    var tenantId = config["Entra:TenantId"];
    var scopes = (config["DocumentsApi:Scopes"] ?? "").Split(' ');

    var httpClientHandler = Utils.GetHttpClientHandler();
    var httpClientFactory = new CustomHttpClientFactory(httpClientHandler);

    var app = ConfidentialClientApplicationBuilder.Create(clientId)
            .WithClientSecret(clientSecret)
            .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
            .WithHttpClientFactory(httpClientFactory)
            .Build();

    var authenticationHandler = new AuthenticationDelegatingHandler(app, scopes)
    {
        InnerHandler = Utils.GetHttpClientHandler()
    };
    var httpClient = new HttpClient(authenticationHandler);

    return new DocumentsServiceClient(httpClient);
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
