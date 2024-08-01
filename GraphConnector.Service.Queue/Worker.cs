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
    private IConnection _connection;
    private IModel _channel;


    public Worker(ILogger<Worker> logger, IConnection connection, GraphServiceClient graphClient)
    {
        _logger = logger;
        _connection = connection;
        _graphClient = graphClient;

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
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var jsonMessage = Encoding.UTF8.GetString(body);
            _logger.LogInformation("Received message: {message}", jsonMessage);

            var message = JsonSerializer.Deserialize<ConnectionMessage>(jsonMessage);
            
            switch (message.Action)
            {
                case ConnectionMessageAction.Create:
                    _logger.LogInformation("Creating connection for {connectorId}", message.ConnectorId);
                    await CreateConnection(message);
                    break;
                case ConnectionMessageAction.Delete:
                    await DeleteConnection();
                    _logger.LogInformation("Deleting connection for {connectorId}", message.ConnectorId);
                    break;
                case ConnectionMessageAction.Status:
                    _logger.LogInformation("Checking status for {connectorId}", message.ConnectorId);
                    break;
            }
        };

        _channel.BasicConsume(queue: "connections",
                             autoAck: true,
                             consumer: consumer);

        return Task.CompletedTask;
    }

    private async Task CreateConnection(ConnectionMessage connectionMessage)
    {
        var externalConnection = ConnectionConfiguration.ExternalConnection;

        await _graphClient.External.Connections
            .PostAsync(externalConnection);
    }

    private async Task DeleteConnection()
    {
        await _graphClient.External
            .Connections[ConnectionConfiguration.ExternalConnection.Id]
            .DeleteAsync();
    }

    public override void Dispose()
    {
        _channel.Close();
        _connection.Close();
        base.Dispose();
    }
}
