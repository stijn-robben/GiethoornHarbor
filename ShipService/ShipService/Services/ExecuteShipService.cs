using ShipService.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace ShipService.Services
{
    public class ExecuteShipService
    {
        private readonly RabbitMqPublisher _eventPublisher;
        private readonly IServiceProvider _serviceProvider;
        private int _amount = 0;

        public ExecuteShipService(RabbitMqPublisher eventPublisher, IServiceProvider serviceProvider)
        {
            _eventPublisher = eventPublisher;
            _serviceProvider = serviceProvider;
        }

        public void Execute(string shippingCompanyName, bool needsService)
        {
            Console.WriteLine("Executing Ship Service...");

            _amount += needsService ? 100 : 50;

            PublishInvoiceEvent(shippingCompanyName, _amount, DateTime.UtcNow);
        }

        private void PublishInvoiceEvent(string shippingCompanyName, int amount, DateTime generatedAt)
        {
            var invoiceEvent = new
            {
                EventType = "ShipServiceInvoice",
                ShipmentCompany = shippingCompanyName,
                Amount = amount,
                GeneratedAt = generatedAt
            };

            try
            {
                _eventPublisher.Publish(invoiceEvent);
                Console.WriteLine($"Published");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString(), $"Error");
                throw;
            }
        }
    }
}
