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
            public int ShipmentCompanyId { get; set; } // Need this to link the company
            public DateTime RentalStart { get; set; }
            public DateTime? RentalEnd { get; set; }
        }

        [HttpPost("{id}/rent")]
        public async Task<IActionResult> RentDock(int id, [FromBody] RentDockRequest request)
        {
            var dock = await _context.Docks
                .Include(d => d.ShipmentCompany)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dock == null)
                return NotFound();

            if (dock.IsRented)
                return BadRequest("Dock is already rented.");

            // Get the shipment company
            var company = await _context.ShipmentCompanies.FindAsync(request.ShipmentCompanyId);
            if (company == null)
                return BadRequest("Shipment company not found.");

            dock.IsRented = true;
            dock.RentalStart = request.RentalStart;
            dock.RentalEnd = request.RentalEnd;
            dock.ShipmentCompany = company;

            await _context.SaveChangesAsync();

            // Publish simple DockRented event (invoicing handled by background service)
            var publisher = new EventPublisher();
            publisher.Publish(new
            {
                EventType = "DockRented",
                DockName = dock.Name,
                ShipmentCompany = company.CompanyName,
                RentalStart = dock.RentalStart,
                RentalEnd = dock.RentalEnd
            });

            return Ok(dock);
        }
    }
}