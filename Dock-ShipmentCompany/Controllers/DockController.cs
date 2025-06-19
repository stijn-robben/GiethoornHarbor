using Dock_ShipmentCompany.Database;
using Dock_ShipmentCompany.Messaging;
using Dock_ShipmentCompany.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dock_ShipmentCompany.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DockController : ControllerBase
    {
        private readonly PortDbContext _context;

        public DockController(PortDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _context.Docks.Include(d => d.ShipmentCompany).ToListAsync());

        [HttpPost]
        public async Task<IActionResult> Create(Dock dock)
        {
            _context.Docks.Add(dock);
            await _context.SaveChangesAsync();



            return CreatedAtAction(nameof(GetAll), new { id = dock.Id }, dock);
        }

        public class RentDockRequest
        {
            public DateTime RentalStart { get; set; }
            public DateTime? RentalEnd { get; set; }
        }

        [HttpPost("{id}/rent")]
        public async Task<IActionResult> RentDock(int id, [FromBody] RentDockRequest request)
        {
            var dock = await _context.Docks.FindAsync(id);
            if (dock == null)
                return NotFound();

            if (dock.IsRented)
                return BadRequest("Dock is already rented.");

            dock.IsRented = true;
            dock.RentalStart = request.RentalStart;
            dock.RentalEnd = request.RentalEnd;

            await _context.SaveChangesAsync();

            // Optional: publish DockRented event here
            var publisher = new EventPublisher();
            publisher.Publish(new
            {
                EventType = "DockInvoice",
                ShipmentCompany = dock.ShipmentCompany.CompanyName,
                ShipmentCompanyEmail = dock.ShipmentCompany.CompanyEmail,
                ShipmentCompanyPhone = dock.ShipmentCompany.CompanyPhone,
                ShipmentCompanyCardNumber = dock.ShipmentCompany.CardNumber,
                RentalStart = dock.RentalStart,
                RentalEnd = dock.RentalEnd

            });


            return Ok(dock);
        }

    }

}
