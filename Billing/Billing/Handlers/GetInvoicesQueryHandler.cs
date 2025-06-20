using Billing.Data;
using Billing.Models;
using System.Collections.Generic;
using System.Linq;

namespace Billing.Handlers
{
    public class GetInvoicesQueryHandler
    {
        private readonly BillingContext _context;

        public GetInvoicesQueryHandler(BillingContext context)
        {
            _context = context;
        }

        public IEnumerable<Invoice> Handle()
        {
            return _context.Invoices.ToList();
        }
    }
}