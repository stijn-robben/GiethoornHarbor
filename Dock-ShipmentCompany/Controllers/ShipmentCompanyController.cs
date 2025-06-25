using Dock_ShipmentCompany.Database;
using Dock_ShipmentCompany.Models;
using Dock_ShipmentCompany.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dock_ShipmentCompany.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShipmentCompanyController : ControllerBase
    {
        private readonly PortDbContext _context;
        private readonly EventPublisher _eventPublisher;

        public ShipmentCompanyController(PortDbContext context, EventPublisher eventPublisher)
        {
            _context = context;
            _eventPublisher = eventPublisher;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _context.ShipmentCompanies.Include(c => c.Docks).ToListAsync());

        [HttpPost]
        public async Task<IActionResult> Create(ShipmentCompany company)
        {
            _context.ShipmentCompanies.Add(company);
            await _context.SaveChangesAsync();

            // Create event object with all shipment company data
            var shipmentCompanyCreatedEvent = new
            {
                EventType = "ShipmentCompanyCreated",
                Timestamp = DateTime.UtcNow,
                Data = new
                {
                    Id = company.Id,
                    CompanyName = company.CompanyName,
                    AddressLine = company.AddressLine,
                    City = company.City,
                    Country = company.Country,
                    CardNumber = company.CardNumber,
                    CompanyPhone = company.CompanyPhone,
                    CompanyEmail = company.CompanyEmail,
                    Docks = company.Docks
                }
            };

            // Publish the event to the dock-events exchange
            _eventPublisher.Publish(shipmentCompanyCreatedEvent);

            return CreatedAtAction(nameof(GetAll), new { id = company.Id }, company);
        }
    }
}