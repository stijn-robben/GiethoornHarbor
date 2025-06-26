using Billing.Data;
using Billing.Events;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Billing.Handlers
{
    public class GetInvoicesQueryHandler
    {
        private readonly BillingContext _context;

        public GetInvoicesQueryHandler(BillingContext context)
        {
            _context = context;
        }

        public IEnumerable<CompanyInvoiceTotal> Handle()
        {
            var events = _context.StoredEvents
                .Where(e => e.EventType == nameof(PaymentRequestedEvent))
                .ToList();

            var paymentEvents = events
                .Select(e => JsonSerializer.Deserialize<PaymentRequestedEvent>(e.Data))
                .Where(e => e != null);

            var totals = paymentEvents
                .GroupBy(e => e.ShippingCompanyName)
                .Select(g => new CompanyInvoiceTotal
                {
                    ShippingCompanyName = g.Key,
                    TotalAmount = g.Sum(e => e.Amount)
                })
                .ToList();

            return totals;
        }
    }

    public class CompanyInvoiceTotal
    {
        public string ShippingCompanyName { get; set; }
        public decimal TotalAmount { get; set; }
    }
}