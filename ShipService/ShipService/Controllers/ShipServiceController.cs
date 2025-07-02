using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShipService.Data;
using ShipService.Models;
using ShipService.Services;

namespace ShipService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShipServiceController : ControllerBase
    {
        private readonly ShipServiceContext _context;
        private readonly ShipServiceManager _shipServiceManager;

        public ShipServiceController(ShipServiceContext context, ShipServiceManager shipServiceManager)
        {
            _context = context;
            _shipServiceManager = shipServiceManager;
        }

        // GET: api/shipservice
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Models.ShipService>>> GetServices()
        {
            return Ok(await _context.ShipServices
                .Include(s => s.Containers)
                .ToListAsync());
        }

        // GET: api/shipservice/containers
        [HttpGet("containers")]
        public async Task<ActionResult<IEnumerable<Container>>> GetAllContainers()
        {
            return Ok(await _context.Containers.ToListAsync());
        }

        // GET: api/shipservice/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Models.ShipService>> GetShipServiceById(int id)
        {
            var shipService = await _context.ShipServices
                .Include(s => s.Containers)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (shipService == null)
                return NotFound();

            return Ok(shipService);
        }

        // POST: api/shipservice/execute
        [HttpPost("execute")]
        public IActionResult Execute()
        {
            int testShipId = 1;
            string testCompany = "TestCompany";

            _shipServiceManager.Execute(testShipId, testCompany);
            return Ok(new { message = $"Ship service execution triggered for ShipId={testShipId}, Company={testCompany}." });
        }

        // POST: api/shipservice/calltruck
        [HttpPost("calltruck")]
        public IActionResult CallTruck()
        {
            Console.WriteLine("[ShipServiceController] Truck has been called for container pickup.");
            return Ok(new { message = "Truck has been called for container pickup." });
        }
    }
}