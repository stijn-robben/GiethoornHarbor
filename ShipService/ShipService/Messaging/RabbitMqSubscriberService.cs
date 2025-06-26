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
                break; // verbinding gelukt
            }
            catch (Exception ex)
            {
                retries++;
                if (retries >= 10)
                    throw new Exception("Kon geen verbinding maken met RabbitMQ", ex);

                Console.WriteLine($"[RabbitMQ] Verbinden mislukt. Opnieuw proberen ({retries}/10)...");
                Thread.Sleep(3000); // 3 seconden wachten
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

            Console.WriteLine("[RabbitMQ] Event ontvangen:");
            Console.WriteLine(message);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var executeShipService = scope.ServiceProvider.GetRequiredService<ExecuteShipService>();

                using var doc = JsonDocument.Parse(message);
                var root = doc.RootElement;

                var eventType = root.GetProperty("EventType").GetString();
                Console.WriteLine($"EventType: {eventType}");

                if (eventType == "ShipArrived")
                {
                    var company = root.GetProperty("Company").GetString();
                    var needsService = root.GetProperty("NeedsService").GetBoolean();

                    Console.WriteLine($"ShipArrived ontvangen van bedrijf: {company}, needsService: {needsService}");

                    executeShipService.Execute(company!, needsService);
                    Console.WriteLine("ExecuteShipService succesvol uitgevoerd");
                }
                else
                {
                    Console.WriteLine($"Onbekend eventtype: {eventType}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fout bij verwerken event: {ex.Message}");
            }
        };

        _channel.BasicConsume("shipservice-queue", autoAck: true, consumer: consumer);

        Console.WriteLine("[RabbitMQ] Wachten op berichten op 'shipservice-queue'...");

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}