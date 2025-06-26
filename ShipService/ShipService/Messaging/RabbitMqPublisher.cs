using RabbitMQ.Client;
using System.Text.Json;
using System.Text;

namespace ShipService.Messaging
{
    public class RabbitMqPublisher
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private bool _disposed = false;

        public RabbitMqPublisher()
        {
            var factory = new ConnectionFactory { HostName = "rabbitmq" };
            int retries = 0;
            while (true)
            {
                try
                {
                    _connection = factory.CreateConnection();
                    _channel = _connection.CreateModel();
                    break; 
                }
                catch (Exception ex)
                {
                    retries++;
                    if (retries >= 10)
                        throw new Exception("Kon geen verbinding maken met RabbitMQ", ex);

                    Console.WriteLine($"[RabbitMQ] Verbinden mislukt. Opnieuw proberen ({retries}/10)...");
                    Thread.Sleep(3000); 
                }
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

                _channel.BasicPublish(exchange: "shipservice-events",
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