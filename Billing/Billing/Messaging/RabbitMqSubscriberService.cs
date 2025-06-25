using Billing.Commands;
using Billing.Data;
using Billing.Handlers;
using Billing.Models;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
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
        _channel.ExchangeDeclare("dock-events", ExchangeType.Fanout);
        _channel.QueueDeclare("billing-queue", durable: false, exclusive: false, autoDelete: false, arguments: null);
        _channel.QueueBind("billing-queue", "dock-events", "");
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
                var context = scope.ServiceProvider.GetRequiredService<BillingContext>();

                // Try to detect event type
                using var doc = JsonDocument.Parse(message);
                var root = doc.RootElement;
                var eventType = root.GetProperty("EventType").GetString();

                if (eventType == "MonthlyDockInvoice")
                {
                    Console.WriteLine($"Received MonthlyDockInvoice event: {message}");
                    var companyName = root.GetProperty("ShipmentCompany").GetString();
                    var amount = root.GetProperty("Amount").GetDecimal();
                    var requestedAt = root.GetProperty("GeneratedAt").GetDateTime();

                    var handler = new RequestPaymentCommandHandler(context);
                    handler.Handle(new RequestPaymentCommand
                    {
                        ShippingCompanyName = companyName,
                        Amount = amount,
                        RequestedAt = requestedAt
                    });
                }
                else if (eventType == "ShipmentCompanyCreated")
                {
                    Console.WriteLine($"Received ShipmentCompanyCreated event: {message}");
                    var companyName = root.GetProperty("CompanyName").GetString();
                    var contactEmail = root.GetProperty("CompanyEmail").GetString();
                    var shipmentCompanyCardNumber = root.GetProperty("CardNumber").GetString();

                    if (!context.shippingCompanies.Any(sc => sc.CompanyName == companyName))
                    {
                        context.shippingCompanies.Add(new ShippingCompany
                        {
                            CompanyName = companyName,
                            ContactEmail = contactEmail,
                            ShipmentCompanyCardNumber = shipmentCompanyCardNumber
                        });
                        context.SaveChanges();
                    }
                }
            }
        };

        _channel.BasicConsume("billing-queue", autoAck: true, consumer: consumer);
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}