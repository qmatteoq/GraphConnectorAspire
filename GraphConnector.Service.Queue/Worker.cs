using System.Text;
using System.Text.Json;
using GraphConnector.Library.Configuration;
using GraphConnector.Library.Enums;
using GraphConnector.Library.Messages;
using Microsoft.Graph;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GraphConnector.Service.Queue;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly GraphServiceClient _graphClient;
    private readonly IConnectionConfiguration _connectionConfiguration;
    private IConnection _connection;
    private IModel _channel;

    public Worker(ILogger<Worker> logger, IConnection connection, GraphServiceClient graphClient, IConnectionConfiguration connectionConfiguration)
    {
        _logger = logger;
        _connection = connection;
        _graphClient = graphClient;
        _connectionConfiguration = connectionConfiguration;

        InitializeRabbitMQ();
    }

    private void InitializeRabbitMQ()
    {
        _channel = _connection.CreateModel();

        //initialize the queue to manage the creation of the connection
        _channel.QueueDeclare(queue: "connections",
                             durable: true,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        _channel.QueueDeclare(queue: "content",
                       durable: true,
                       exclusive: false,
                       autoDelete: false,
                       arguments: null);

        //initialize the queue to manage the status of the creation operation
        _channel.QueueDeclare(queue: "operations",
                       durable: true,
                       exclusive: false,
                       autoDelete: false,
                       arguments: null);
    }


    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        //consume the connection queue and process the messages
        var connection = new EventingBasicConsumer(_channel);
        connection.Received += Connection_Received;

        _channel.BasicConsume(queue: "connections",
                             autoAck: true,
                             consumer: connection);

        return Task.CompletedTask;
    }

    #region Connection
    private async void Connection_Received(object? sender, BasicDeliverEventArgs ea)
    {
        var body = ea.Body.ToArray();
        var jsonMessage = Encoding.UTF8.GetString(body);
        _logger.LogInformation("Received connection message: {message}", jsonMessage);

        var message = JsonSerializer.Deserialize<ConnectionMessage>(jsonMessage);

        switch (message.Action)
        {
            case ConnectionAction.Create:
                _logger.LogInformation("Creating connection for {connectorId}", message.ConnectorId);
                //if it's a creation request, trigger the creation of the connection and the schema
                await CreateConnection(message.ConnectorId, message.ConnectorName, message.ConnectorDescription);
                await CreateSchema(message.ConnectorId, message.FeedUrl);
                break;
            case ConnectionAction.Delete:
                await DeleteConnection(message.ConnectorId);
                _logger.LogInformation("Deleting connection for {connectorId}", message.ConnectorId);
                break;
            case ConnectionAction.Status:
                _logger.LogInformation("Checking status for {connectorId}", message.ConnectorId);
                break;
        }
    }

    private async Task CreateConnection(string connectorId, string connectorName, string connectorDescription)
    {
        OperationStatusMessage message = new()
        {
            Status = OperationStatus.InProgress,
            LastStatusDate = DateTime.Now
        };

        var jsonMessage = JsonSerializer.Serialize(message);

        var body = Encoding.UTF8.GetBytes(jsonMessage);

        _channel.BasicPublish(exchange: string.Empty,
             routingKey: "operations",
             basicProperties: null,
             body: body);

        //get the JSON that descibes the connection
        var externalConnection = _connectionConfiguration.GetExternalConnection(connectorId, connectorName, connectorDescription);

        //call the Microsoft Graph to create the connection
        var result = await _graphClient.External.Connections
            .PostAsync(externalConnection);
    }

    private async Task DeleteConnection(string connectorId)
    {
        //delete a Graph Connector
        await _graphClient.External
            .Connections[connectorId]
            .DeleteAsync();
    }

    #endregion

    #region Schema
    private async Task CreateSchema(string connectorId, string feedUrl)
    {
        //get the JSON that describes the schema
        var schema = _connectionConfiguration.GetSchema();

        //call the Microsot Graph to create the schema
        var schemaRequest = _graphClient.External
            .Connections[connectorId]
            .Schema
            .ToPatchRequestInformation(schema);
        var httpRequestMessage = await _graphClient.RequestAdapter
            .ConvertToNativeRequestAsync<HttpRequestMessage>(schemaRequest);
        if (httpRequestMessage is null)
        {
            _logger.LogError("httpRequestMessage is null");
            return;
        }

        var httpClient = Utils.GetHttpClient();
        var res = await httpClient.SendAsync(httpRequestMessage);
        //get the location header, which contains the URL that you can use to check the operation status (schema creation can take between 5 and 15 minutes)
        var location = res.Headers.GetValues("location")?.FirstOrDefault();

        if (string.IsNullOrEmpty(location))
        {
            _logger.LogError("Schema operation status location is empty");
            return;
        }

        //get the operation id from the URL
        Uri uri = new Uri(location);
        string[] segments = uri.Segments;
        string operationId = segments.Last().Trim('/');

        //start a timer to check every minute the status of the operation. This is because you can start uploading the items only when the schema has been created.
        System.Timers.Timer timer = new System.Timers.Timer(TimeSpan.FromMinutes(1));
        timer.Elapsed += async (sender, e) =>
        {
            //call the Microsoft Graph to check the status of the creation operation
            var response = await _graphClient.External
                .Connections[connectorId]
                .Operations[operationId]
                .GetAsync();

            //if the schema has been created, go on and upload the content from the external data source
            if (response.Status == Microsoft.Graph.Models.ExternalConnectors.ConnectionOperationStatus.Completed)
            {
                timer.Stop();
                await UploadContent(connectorId, feedUrl);

                //clean the queue and send a message that the operation is completed
                _channel.QueuePurge("operations");

                OperationStatusMessage message = new()
                {
                    Status = OperationStatus.Completed,
                    LastStatusDate = DateTime.Now
                };

                var jsonMessage = JsonSerializer.Serialize(message);

                var body = Encoding.UTF8.GetBytes(jsonMessage);

                _channel.BasicPublish(exchange: string.Empty,
                     routingKey: "operations",
                     basicProperties: null,
                     body: body);
            }
            else
            {
                //if the operation is still in progress, clean the queue and send an updated message with an updated date and time
                _channel.QueuePurge("operations");

                OperationStatusMessage message = new()
                {
                    Status = OperationStatus.InProgress,
                    LastStatusDate = DateTime.Now
                };

                var jsonMessage = JsonSerializer.Serialize(message);

                var body = Encoding.UTF8.GetBytes(jsonMessage);

                _channel.BasicPublish(exchange: string.Empty,
                     routingKey: "operations",
                     basicProperties: null,
                     body: body);
            }
        };

        //start the timer
        timer.Start();
    }

    #endregion

    #region Content

    private async Task UploadContent(string connectordId, string url)
    {
        //parse the RSS feed to get the items
        RssParser parser = new RssParser();
        var items = parser.ParseRss(url);

        foreach (var rssItem in items)
        {            
            //conver the RSS item into an ExternalItem, which is used by the Microsoft Graph to represent an external content
            var externalItem = rssItem.ToExternalItem();
            _logger.LogInformation($"Uploading item with id {externalItem.Id}");

            if (externalItem != null && !string.IsNullOrEmpty(externalItem.Id))
            {
                try
                {
                    //call the Microsoft Graph to upload the item
                    await _graphClient.External
                       .Connections[connectordId]
                       .Items[externalItem.Id]
                       .PutAsync(externalItem);
                }
                catch (Exception exc)
                {
                    _logger.LogError(exc, $"Error uploading item with id {externalItem.Id}");
                }
            }
        }
    }

    #endregion

    public override void Dispose()
    {
        _channel.Close();
        _connection.Close();
        base.Dispose();
    }
}