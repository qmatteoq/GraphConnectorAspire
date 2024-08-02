using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var messaging = builder.AddRabbitMQ("messaging").WithManagementPlugin();

var api = builder.AddProject<Projects.GraphConnector_Api_Queue>("graphconnector-api-queue")
    .WithReference(messaging);

var queue = builder.AddProject<Projects.GraphConnector_Service_Queue>("graphconnector-service-queue")
    .WithEnvironment("CustomProxy", "127.0.0.1:8000")
    .WithReference(messaging);

builder.AddProject<Projects.GraphConnector_UI>("graphconnector-ui")
    .WithExternalHttpEndpoints()
    .WithReference(api);
    
builder.Build().Run();
