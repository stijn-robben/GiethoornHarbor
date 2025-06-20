using Billing.Commands;
using Billing.Data;
using Billing.Events;
using Billing.Handlers;
using Billing.Models;
using Microsoft.AspNetCore.Mvc;

namespace Billing.Controllers
{
    public class BillingController : Controller
    {
        private readonly BillingContext _context;

        public BillingController(BillingContext context)
        {
            _context = context;
        }

        [HttpPost("request-payment")]
        public IActionResult RequestPayment([FromBody] RequestPaymentCommand command)
        {
            var handler = new RequestPaymentCommandHandler(_context);
            var success = handler.Handle(command);
            if (!success)
                return BadRequest("Unknown shipping company.");
            return Ok(new { message = "PaymentRequested event stored." });
        }

        [HttpGet("invoices")]
        public ActionResult<IEnumerable<Invoice>> GetAllInvoices()
        {
            var handler = new GetInvoicesQueryHandler(_context);
            var invoices = handler.Handle();
            return Ok(invoices);
        }
    }
}
