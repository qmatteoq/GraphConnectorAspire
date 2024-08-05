using GraphConnector.Library.Enums;
using GraphConnector.Library.Messages;
using GraphConnector.Library.Responses;
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

//endpoint to call to trigger the creation of the Graph Connector
app.MapPost("/createConnection", ([FromServices] IConnection connection, [FromServices] ILogger<Program> logger, [FromBody] QueueConnectionRequest connectionRequest) =>
{
    //create a message for the queue with the information required to create the Graph Connector
    ConnectionMessage message = new ConnectionMessage
    {
        Action = connectionRequest.Action,
        ConnectorId = connectionRequest.ConnectorId,
        ConnectorDescription = connectionRequest.ConnectorDescription,
        ConnectorName = connectionRequest.ConnectorName,
        FeedUrl = connectionRequest.FeedUrl
    };

    using (var channel = connection.CreateModel())
    {
        var jsonMessage = JsonSerializer.Serialize(message);

        var body = Encoding.UTF8.GetBytes(jsonMessage);

        //send the message to the queue called "connections"
        channel.BasicPublish(exchange: string.Empty,
                             routingKey: "connections",
                             basicProperties: null,
                             body: body);

        logger.LogInformation($"Sending create connection request with payload: {jsonMessage}");
    }

    return TypedResults.Ok();
})
.WithName("CreateConnection")
.WithOpenApi();

app.MapPost("/createSchema", ([FromServices] IConnection connection, [FromServices] ILogger<Program> logger, [FromBody] QueueConnectionRequest schemaRequest) =>
{
    ConnectionMessage message = new ConnectionMessage
    {
        Action = schemaRequest.Action,
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

//app.MapPost("/uploadContent", ([FromServices] IConnection connection, [FromServices] ILogger<Program> logger, [FromBody] QueueContentRequest contentMessage) =>
//{
//    ContentAction action;
//    switch (contentMessage.Action)
//    {
//        case "create":
//            action = ContentAction.Create;
//            break;
//        case "delete":
//            action = ContentAction.Delete;
//            break;
//        default:
//            action = ContentAction.Create;
//            break;
//    }

//    ContentMessage message = new ContentMessage
//    {
//        Action = action,
//        Url = contentMessage.FeedUrl,
//        ConnectorId = contentMessage.ConnectorId
//    };

//    using (var channel = connection.CreateModel())
//    {
//        var jsonMessage = JsonSerializer.Serialize(message);

//        var body = Encoding.UTF8.GetBytes(jsonMessage);

//        channel.BasicPublish(exchange: string.Empty,
//                             routingKey: "content",
//                             basicProperties: null,
//                             body: body);

//        logger.LogInformation($"Sent message: {jsonMessage}");
//    }

//    return TypedResults.Ok();
//})
//.WithName("UploadContent")
//.WithOpenApi();

//endpoint to check the progress of the Graph Connector creation operation
app.MapGet("/checkOperationProgress", ([FromServices] IConnection connection, [FromServices] ILogger<Program> logger) =>
{
    using (var channel = connection.CreateModel())
    {
        OperationStatusResponse response;
        //check if there's a message in the queue called "operations"
        var message = channel.BasicGet("operations", false);
        if (message != null)
        {
            var body = message.Body.ToArray();
            var jsonMessage = Encoding.UTF8.GetString(body);

            var statusMessage = JsonSerializer.Deserialize<OperationStatusMessage>(jsonMessage);

            //generate an API response using the information from the message
            response = new()
            {
                Status = statusMessage.Status,
                LastStatusDate = statusMessage.LastStatusDate
            };

            //if the message describes that the operation is completed, acknowledge the message so that it gets deleted from the queue
            if (response.Status == OperationStatus.Completed)
            {
                channel.BasicAck(message.DeliveryTag, false);
            }
        }
        else
        {
            //if there are no messages, it means the operation is still in progress
            response = new()
            {
                Status = OperationStatus.InProgress,
                LastStatusDate = DateTimeOffset.Now
            };
        }

        return TypedResults.Ok(response);
    }
})
.WithName("CheckOperationProgress")
.WithOpenApi();

app.Run();
