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
            // Get all PaymentRequestedEvents
            var requestedEvents = _context.StoredEvents
                .Where(e => e.EventType == nameof(PaymentRequestedEvent))
                .ToList();

            var paymentRequested = requestedEvents
                .Select(e => JsonSerializer.Deserialize<PaymentRequestedEvent>(e.Data))
                .Where(e => e != null);

            // Get all PaymentMadeEvents
            var paidEvents = _context.StoredEvents
                .Where(e => e.EventType == nameof(PaymentMadeEvent))
                .ToList();

            var paymentMade = paidEvents
                .Select(e => JsonSerializer.Deserialize<PaymentMadeEvent>(e.Data))
                .Where(e => e != null);

            // Group and sum requested amounts
            var requestedTotals = paymentRequested
                .GroupBy(e => e.ShippingCompanyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(e => e.Amount)
                );

            // Group and sum paid amounts
            var paidTotals = paymentMade
                .GroupBy(e => e.ShippingCompanyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(e => e.Amount)
                );

            // Combine to get open amount per company
            var allCompanies = requestedTotals.Keys.Union(paidTotals.Keys);

            var result = allCompanies
                .Select(company => new CompanyInvoiceTotal
                {
                    ShippingCompanyName = company,
                    TotalAmount = requestedTotals.GetValueOrDefault(company, 0) - paidTotals.GetValueOrDefault(company, 0)
                })
                .ToList();

            return result;
        }
    }

    public class CompanyInvoiceTotal
    {
        public string ShippingCompanyName { get; set; }
        public decimal TotalAmount { get; set; }
    }
}