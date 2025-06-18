using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace HarborManagementService.Messaging
{
    public class EventPublisher
    {
        private readonly IModel _channel;

        public EventPublisher()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            var connection = factory.CreateConnection();
            _channel = connection.CreateModel();

            _channel.ExchangeDeclare(exchange: "harbor-events", type: ExchangeType.Fanout);
        }

        public void Publish(object @event)
        {
            var json = JsonSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(json);

            _channel.BasicPublish(exchange: "harbor-events",
                                  routingKey: "",
                                  basicProperties: null,
                                  body: body);
        }
    }
}
