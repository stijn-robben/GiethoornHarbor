using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

public class SimpleTestSubscriberService : BackgroundService
{
    private IConnection _connection;
    private IModel _channel;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare("harbor-events", ExchangeType.Fanout);

            var queueName = "test-queue";
            _channel.QueueDeclare(queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
            _channel.QueueBind(queue: queueName, exchange: "harbor-events", routingKey: "");

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($"📥 Ontvangen event: {message}");
            };

            _channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

            Console.WriteLine("[✔] SimpleTestSubscriber is actief en luistert.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[❌] Fout in SimpleTestSubscriber: {ex.Message}");
        }

        // laat service eindeloos doorlopen
        return Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}
