using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WaterManagement.Handlers
{
    public class ShipMessageHandler : BackgroundService
    {
        private readonly ILogger<ShipMessageHandler> _logger;
        private IConnection _connection;
        private IModel _channel;

        public ShipMessageHandler(ILogger<ShipMessageHandler> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory() { HostName = "rabbitmq" };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            string exchangeName = "harbor-events";
            string queueName = "water-queue";

            _channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Fanout, durable: false);
            _channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false);
            _channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: "");

            _logger.LogInformation("ShipMessageHandler luistert naar RabbitMQ.");
            Console.WriteLine("ShipMessageHandler luistert naar RabbitMQ.");

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                _logger.LogInformation("Ontvangen event: {message}", message);
                Console.WriteLine($"Ontvangen event: {message}");
            };

            _channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

            // Wacht tot service wordt gestopt
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ShipMessageHandler wordt gestopt.");
            _channel?.Close();
            _connection?.Close();
            return base.StopAsync(cancellationToken);
        }
    }
}