using System.Text;
using System.Text.Json;
using GraphConnector.Library.Configuration;
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
        _channel.QueueDeclare(queue: "connections",
                             durable: true,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        _channel.QueueDeclare(queue: "schema",
                               durable: true,
                               exclusive: false,
                               autoDelete: false,
                               arguments: null);

        _channel.QueueDeclare(queue: "content",
                       durable: true,
                       exclusive: false,
                       autoDelete: false,
                       arguments: null);
    }


    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        var connection = new EventingBasicConsumer(_channel);
        connection.Received += Connection_Received;

        var schema = new EventingBasicConsumer(_channel);
        schema.Received += Schema_Received;

        var content = new EventingBasicConsumer(_channel);
        content.Received += Content_Received;

        _channel.BasicConsume(queue: "connections",
                             autoAck: true,
                             consumer: connection);

        _channel.BasicConsume(queue: "schema",
                             autoAck: true,
                             consumer: schema);

        _channel.BasicConsume(queue: "content",
                             autoAck: true,
                             consumer: content);

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
            case ConnectionMessageAction.Create:
                _logger.LogInformation("Creating connection for {connectorId}", message.ConnectorId);
                await CreateConnection(message.ConnectorId, message.ConnectorName, message.ConnectorDescription);
                break;
            case ConnectionMessageAction.Delete:
                await DeleteConnection(message.ConnectorId);
                _logger.LogInformation("Deleting connection for {connectorId}", message.ConnectorId);
                break;
            case ConnectionMessageAction.Status:
                _logger.LogInformation("Checking status for {connectorId}", message.ConnectorId);
                break;
        }
    }

    private async Task CreateConnection(string connectorId, string connectorName, string connectorDescription)
    {
        var externalConnection = _connectionConfiguration.GetExternalConnection(connectorId, connectorName, connectorDescription);

        await _graphClient.External.Connections
            .PostAsync(externalConnection);
    }

    private async Task DeleteConnection(string connectorId)
    {
        await _graphClient.External
            .Connections[connectorId]
            .DeleteAsync();
    }

    #endregion

    #region Schema

    private async void Schema_Received(object? sender, BasicDeliverEventArgs ea)
    {
        var body = ea.Body.ToArray();
        var jsonMessage = Encoding.UTF8.GetString(body);
        _logger.LogInformation("Received queue message: {message}", jsonMessage);

        var message = JsonSerializer.Deserialize<ConnectionMessage>(jsonMessage);

        switch (message.Action)
        {
            case ConnectionMessageAction.Create:
                _logger.LogInformation("Creating schema for {connectorId}", message.ConnectorId);
                await CreateSchema(message.ConnectorId);
                break;
            case ConnectionMessageAction.Delete:
                //await DeleteConnection();
                _logger.LogInformation("Deleting schema for {connectorId}", message.ConnectorId);
                break;
            case ConnectionMessageAction.Status:
                _logger.LogInformation("Checking status for {connectorId}", message.ConnectorId);
                break;
        }
    }

    private async Task CreateSchema(string connectorId)
    {
        var schema = _connectionConfiguration.GetSchema();

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
        var location = res.Headers.GetValues("location")?.FirstOrDefault();
        if (string.IsNullOrEmpty(location))
        {
            _logger.LogError("Schema operation status location is empty");
            return;
        }
    }

    #endregion

    #region Content

    private async void Content_Received(object? sender, BasicDeliverEventArgs ea)
    {
        var body = ea.Body.ToArray();
        var jsonMessage = Encoding.UTF8.GetString(body);
        _logger.LogInformation("Received queue message: {message}", jsonMessage);

        var message = JsonSerializer.Deserialize<ContentMessage>(jsonMessage);

        switch (message.Action)
        {
            case ContentAction.Create:
                _logger.LogInformation($"Uploading content from RSS feed: {message.Url}");
                await UploadContent(message.ConnectorId, message.Url);
                break;
            case ContentAction.Delete:
                //await DeleteConnection();
                _logger.LogInformation($"Uploading content from RSS feed: {message.Url}");
                break;
          
        }
    }

    private async Task UploadContent(string connectordId, string url)
    {
        RssParser parser = new RssParser();
        var items = parser.ParseRss(url);

        foreach (var rssItem in items)
        {            
            var externalItem = rssItem.ToExternalItem();
            _logger.LogInformation($"Uploading item with id {externalItem.Id}");

            if (externalItem != null && !string.IsNullOrEmpty(externalItem.Id))
            {
                try
                {
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