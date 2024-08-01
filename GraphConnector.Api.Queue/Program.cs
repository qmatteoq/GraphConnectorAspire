using GraphConnector.Library.Configuration;
using GraphConnector.Library.Messages;
using GraphConnector.Library.Requests;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph.Models;
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

app.MapPost("/queueConnection", ([FromServices] IConnection connection, [FromServices] ILogger<Program> logger, [FromBody] QueueConnectionRequest connectionMessage) =>
{
    ConnectionMessageAction action;
    switch (connectionMessage.Action)
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
        ConnectorId = ConnectionConfiguration.ExternalConnection.Id
    };

    using (var channel = connection.CreateModel())
    {
        channel.QueueDeclare(queue: "connections",
                             durable: true,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

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

app.Run();
