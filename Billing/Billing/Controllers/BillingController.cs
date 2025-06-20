using Billing.Data;
using Billing.Models;
using Microsoft.AspNetCore.Mvc;

namespace Billing.Controllers
{
    public class BillingController : Controller
    {
        public readonly BillingContext _context;

        public BillingController(BillingContext context)
        {
            _context = context;
        }

        [HttpGet]
        public ActionResult<IEnumerable<Invoice>> GetAllInvoices()
        {
            return Ok(_context.Invoices.ToList());
        }
    }
}
