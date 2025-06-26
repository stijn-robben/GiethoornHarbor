using Billing.Commands;
using Billing.Data;
using Billing.Events;
using Billing.Models;
using System.Linq;

namespace Billing.Handlers
{
    public class MakePaymentCommandHandler
    {
        private readonly BillingContext _context;

        public MakePaymentCommandHandler(BillingContext context)
        {
            _context = context;
        }

        public bool Handle(MakePaymentCommand command)
        {
            // Validate shipping company exists
            var exists = _context.shippingCompanies.Any(sc => sc.CompanyName == command.ShippingCompanyName);
            if (!exists)
                return false;

            // Store payment event
            var paymentEvent = new PaymentMadeEvent
            {
                ShippingCompanyName = command.ShippingCompanyName,
                Amount = command.Amount,
                PaidAt = command.PaidAt
            };

            var storedEvent = new StoredEvent
            {
                EventType = nameof(PaymentMadeEvent),
                Data = System.Text.Json.JsonSerializer.Serialize(paymentEvent),
                OccurredAt = DateTime.UtcNow
            };

            _context.StoredEvents.Add(storedEvent);
            _context.SaveChanges();
            return true;
        }
    }
}