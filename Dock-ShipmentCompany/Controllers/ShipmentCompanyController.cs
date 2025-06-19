using Dock_ShipmentCompany.Database;
using Dock_ShipmentCompany.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dock_ShipmentCompany.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShipmentCompanyController : ControllerBase
    {
        private readonly PortDbContext _context;

        public ShipmentCompanyController(PortDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _context.ShipmentCompanies.Include(c => c.Docks).ToListAsync());

        [HttpPost]
        public async Task<IActionResult> Create(ShipmentCompany company)
        {
            _context.ShipmentCompanies.Add(company);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAll), new { id = company.Id }, company);
        }
    }

}
