using GraphConnector.Library.Configuration;
using GraphConnector.Library.Messages;
using GraphConnector.Library.Requests;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddRabbitMQClient("messaging");

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/createConnection", ([FromServices] IConnection connection, [FromServices] ILogger<Program> logger, [FromBody] QueueConnectionRequest connectionRequest) =>
{
    ConnectionMessageAction action;
    switch (connectionRequest.Action)
    {
        case "create":
            action = ConnectionMessageAction.Create;
            break;
        case "delete":
            action = ConnectionMessageAction.Delete;
            break;
        case "status":
        default:
            action = ConnectionMessageAction.Status;
            break;
    }

    ConnectionMessage message = new ConnectionMessage
    {
        Action = action,
        ConnectorId = connectionRequest.ConnectorId,
        ConnectorDescription = connectionRequest.ConnectorDescription,
        ConnectorName = connectionRequest.ConnectorName
    };

    using (var channel = connection.CreateModel())
    {
        var jsonMessage = JsonSerializer.Serialize(message);

        var body = Encoding.UTF8.GetBytes(jsonMessage);

        channel.BasicPublish(exchange: string.Empty,
                             routingKey: "connections",
                             basicProperties: null,
                             body: body);

        logger.LogInformation($"Sent message: {jsonMessage}");
    }

    return TypedResults.Ok();
})
.WithName("CreateConnection")
.WithOpenApi();

app.MapPost("/createSchema", ([FromServices] IConnection connection, [FromServices] ILogger<Program> logger, [FromBody] QueueConnectionRequest schemaRequest) =>
{
    ConnectionMessageAction action;
    switch (schemaRequest.Action)
    {
        case "create":
            action = ConnectionMessageAction.Create;
            break;
        case "delete":
            action = ConnectionMessageAction.Delete;
            break;
        case "status":
        default:
            action = ConnectionMessageAction.Status;
            break;
    }

    ConnectionMessage message = new ConnectionMessage
    {
        Action = action,
        ConnectorId = schemaRequest.ConnectorId
    };

    using (var channel = connection.CreateModel())
    {

        var jsonMessage = JsonSerializer.Serialize(message);

        var body = Encoding.UTF8.GetBytes(jsonMessage);

        channel.BasicPublish(exchange: string.Empty,
                             routingKey: "schema",
                             basicProperties: null,
                             body: body);

        logger.LogInformation($"Sent message: {jsonMessage}");
    }

    return TypedResults.Ok();
})
.WithName("CreateSchema")
.WithOpenApi();

app.MapPost("/uploadContent", ([FromServices] IConnection connection, [FromServices] ILogger<Program> logger, [FromBody] QueueContentRequest contentMessage) =>
{
    ContentAction action;
    switch (contentMessage.Action)
    {
        case "create":
            action = ContentAction.Create;
            break;
        case "delete":
            action = ContentAction.Delete;
            break;
        default:
            action = ContentAction.Create;
            break;
    }

    ContentMessage message = new ContentMessage
    {
        Action = action,
        Url = contentMessage.FeedUrl,
        ConnectorId = contentMessage.ConnectorId
    };

    using (var channel = connection.CreateModel())
    {
        var jsonMessage = JsonSerializer.Serialize(message);

        var body = Encoding.UTF8.GetBytes(jsonMessage);

        channel.BasicPublish(exchange: string.Empty,
                             routingKey: "content",
                             basicProperties: null,
                             body: body);

        logger.LogInformation($"Sent message: {jsonMessage}");
    }

    return TypedResults.Ok();
})
.WithName("UploadContent")
.WithOpenApi();

app.Run();
