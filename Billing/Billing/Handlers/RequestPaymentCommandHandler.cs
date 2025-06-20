using Billing.Commands;
using Billing.Data;
using Billing.Events;
using Billing.Models;
using System.Linq;

namespace Billing.Handlers
{
    public class RequestPaymentCommandHandler
    {
        private readonly BillingContext _context;

        public RequestPaymentCommandHandler(BillingContext context)
        {
            _context = context;
        }

        public bool Handle(RequestPaymentCommand command)
        {
            // Validate shipping company exists
            var exists = _context.shippingCompanies.Any(sc => sc.CompanyName == command.ShippingCompanyName);
            if (!exists)
                return false;

            // Store event (event sourcing)
            var paymentEvent = new PaymentRequestedEvent
            {
                ShippingCompanyName = command.ShippingCompanyName,
                Amount = command.Amount,
                RequestedAt = command.RequestedAt
            };

            var storedEvent = new StoredEvent
            {
                EventType = nameof(PaymentRequestedEvent),
                Data = System.Text.Json.JsonSerializer.Serialize(paymentEvent),
                OccurredAt = DateTime.UtcNow
            };

            _context.StoredEvents.Add(storedEvent);
            _context.SaveChanges();
            return true;
        }
    }
}