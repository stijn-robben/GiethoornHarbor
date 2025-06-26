using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ShipService.Services;
using System.Text;
using System.Text.Json;

public class RabbitMqSubscriberService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private IConnection _connection;
    private IModel _channel;
    private readonly ExecuteShipService _executeShipService;

    public RabbitMqSubscriberService(IServiceProvider serviceProvider, ExecuteShipService executeShipService)
    {
        _serviceProvider = serviceProvider;
        _executeShipService = executeShipService;
        var factory = new ConnectionFactory() { HostName = "rabbitmq" };
        int retries = 0;
        while (true)
        {
            try
            {
                _connection = factory.CreateConnection();
                break;
            }
            catch (Exception ex)
            {
                retries++;
                if (retries >= 5)
                    throw new Exception("Failed to connect to RabbitMQ after 5 attempts", ex);

                Console.WriteLine($"[RabbitMQ] Connection failed. Retrying ({retries}/5)...");
                Thread.Sleep(2000);
            }
        }
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare("harbor-events", ExchangeType.Fanout);
        _channel.QueueDeclare("shipservice-queue", durable: false, exclusive: false, autoDelete: false, arguments: null);
        _channel.QueueBind("shipservice-queue", "harbor-events", "");
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            using (var scope = _serviceProvider.CreateScope())
            {
                //var context = scope.ServiceProvider.GetRequiredService<BillingContext>();

                // Try to detect event type
                using var doc = JsonDocument.Parse(message);
                var root = doc.RootElement;
                var eventType = root.GetProperty("EventType").GetString();

                if (eventType == "ShipArrived")
                {
                    Console.WriteLine($"Ship arrived event received {message}");
                    var shippingCompanyName = root.GetProperty("Company").GetString();
                    var needsService = root.GetProperty("NeedsService").GetBoolean();

                    // TODO Process the event service
                    _executeShipService.Execute(shippingCompanyName, needsService);
                }
            }
        };

        _channel.BasicConsume("shipservice-queue", autoAck: true, consumer: consumer);
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}