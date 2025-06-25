using RabbitMQ.Client;
using System.Text.Json;
using System.Text;

namespace Dock_ShipmentCompany.Messaging
{
    public class EventPublisher : IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private bool _disposed = false;

        public EventPublisher()
        {
            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = "rabbitmq"
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _channel.ExchangeDeclare(exchange: "dock-events", type: ExchangeType.Fanout);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to RabbitMQ: {ex.Message}");
                throw;
            }
        }

        public void Publish(object @event)
        {
            if (_disposed || _channel?.IsOpen != true)
            {
                throw new InvalidOperationException("Publisher is disposed or channel is closed");
            }

            try
            {
                var json = JsonSerializer.Serialize(@event);
                var body = Encoding.UTF8.GetBytes(json);

                _channel.BasicPublish(exchange: "dock-events",
                                      routingKey: "",
                                      basicProperties: null,
                                      body: body);

                Console.WriteLine($"Published event: {json}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to publish event: {ex.Message}");
                throw;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _channel?.Close();
                _channel?.Dispose();
                _connection?.Close();
                _connection?.Dispose();
                _disposed = true;
            }
        }
    }
}