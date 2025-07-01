using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using WaterManagement.Services;

namespace WaterManagement.Handlers
{
    public class ShipMessageHandler : BackgroundService
    {
        private readonly ILogger<ShipMessageHandler> _logger;
        private readonly IServiceProvider _serviceProvider;
        private IConnection? _connection;
        private IModel? _channel;

        public ShipMessageHandler(ILogger<ShipMessageHandler> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ConnectWithRetry(stoppingToken);

                    if (_connection?.IsOpen == true && _channel?.IsOpen == true)
                    {
                        _logger.LogInformation("ShipMessageHandler listening to RabbitMQ.");
                        await ListenForMessages(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in message handler. Retrying in 30 seconds...");
                    await Task.Delay(30000, stoppingToken);
                }
            }
        }

        private async Task ConnectWithRetry(CancellationToken stoppingToken)
        {
            const int maxRetries = 5;
            const int retryDelay = 5000;

            for (int i = 0; i < maxRetries && !stoppingToken.IsCancellationRequested; i++)
            {
                try
                {
                    _logger.LogInformation($"Attempting to connect to RabbitMQ (attempt {i + 1}/{maxRetries})...");

                    var factory = new ConnectionFactory
                    {
                        HostName = "rabbitmq",
                        Port = 5672,
                        UserName = "guest",
                        Password = "guest"
                    };

                    _connection = factory.CreateConnection();
                    _channel = _connection.CreateModel();

                    string exchangeName = "harbor-events";
                    string queueName = "water-queue";

                    _channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Fanout, durable: false);
                    _channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false);
                    _channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: "");

                    _logger.LogInformation("Successfully connected to RabbitMQ!");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"RabbitMQ connection failed (attempt {i + 1}/{maxRetries}): {ex.Message}");

                    if (i == maxRetries - 1)
                    {
                        _logger.LogError("Max retries reached. Running without RabbitMQ connection.");
                        return;
                    }

                    await Task.Delay(retryDelay, stoppingToken);
                }
            }
        }

        private async Task ListenForMessages(CancellationToken stoppingToken)
        {
            if (_channel == null) return;

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                _logger.LogInformation("Received event: {message}", message);

                await ProcessMessage(message);
            };

            _channel.BasicConsume(queue: "water-queue", autoAck: true, consumer: consumer);

            while (!stoppingToken.IsCancellationRequested && _connection?.IsOpen == true)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task ProcessMessage(string message)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var waterQualityService = scope.ServiceProvider.GetRequiredService<WaterQualityService>();

                using var doc = JsonDocument.Parse(message);
                var root = doc.RootElement;

                JsonElement eventProperty;
                bool hasEvent = root.TryGetProperty("Event", out eventProperty) ||
                                root.TryGetProperty("EventType", out eventProperty);

                if (hasEvent)
                {
                    var eventType = eventProperty.GetString();

                    if (eventType == "arrived" || eventType == "ShipArrived")
                    {
                        await waterQualityService.HandleShipArrivedAsync();
                        _logger.LogInformation("Water quality updated for ship arrival");
                    }
                    else if (eventType == "departed" || eventType == "ShipDeparted")
                    {
                        await waterQualityService.HandleShipDepartedAsync();
                        _logger.LogInformation("Water quality updated for ship departure");
                    }
                    else
                    {
                        _logger.LogWarning("Unknown event type: {eventType}", eventType);
                    }
                }
                else
                {
                    _logger.LogWarning("Message missing Event/EventType property: {message}", message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message: {message}", message);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ShipMessageHandler stopping.");
            _channel?.Close();
            _connection?.Close();
            return base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}