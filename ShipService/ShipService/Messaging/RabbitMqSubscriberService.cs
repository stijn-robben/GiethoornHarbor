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

    public RabbitMqSubscriberService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        var factory = new ConnectionFactory { HostName = "rabbitmq" };
        int retries = 0;
        while (true)
        {
            try
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                _channel.ExchangeDeclare(exchange: "shipservice-events", type: ExchangeType.Fanout, durable: false, autoDelete: false);

                break; // verbinding gelukt
            }
            catch (Exception ex)
            {
                retries++;
                if (retries >= 10)
                    throw new Exception("Failed to connect to RabbitMQ after 10 attempts", ex);

                Console.WriteLine($"[RabbitMQ] Connection failed. Retrying ({retries}/10)...");
                Thread.Sleep(3000);
            }
        }
        _channel.ExchangeDeclare("harbor-events", ExchangeType.Fanout);
        _channel.QueueDeclare("shipservice-queue", durable: false, exclusive: false, autoDelete: false);
        _channel.QueueBind("shipservice-queue", "harbor-events", "");
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            Console.WriteLine("[RabbitMQ] Event received:");
            Console.WriteLine(message);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var executeShipService = scope.ServiceProvider.GetRequiredService<ShipServiceManager>();

                using var doc = JsonDocument.Parse(message);
                var root = doc.RootElement;

                var eventType = root.GetProperty("EventType").GetString();
                Console.WriteLine($"[RabbitMQ] EventType: {eventType}");

                if (eventType == "ShipArrived")
                {
                    var shipId = root.GetProperty("ShipId").GetInt32();
                    var company = root.GetProperty("Company").GetString();
                    var needsService = root.GetProperty("NeedsService").GetBoolean();

                    Console.WriteLine($"[RabbitMQ] ShipArrived event received: ShipId={shipId}, Company={company}, NeedsService={needsService}");

                    if (needsService)
                    {
                        executeShipService.Execute(shipId, company);
                        Console.WriteLine("[RabbitMQ] ExecuteShipService executed successfully.");
                    }
                }
                else
                {
                    Console.WriteLine($"[RabbitMQ] Unknown event type: {eventType}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RabbitMQ] Error processing event: {ex}");
            }
        };

        _channel.BasicConsume("shipservice-queue", autoAck: true, consumer: consumer);

        Console.WriteLine("[RabbitMQ] Waiting for messages on 'shipservice-queue'...");

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}