using HarborManagementService.Data;
using HarborManagementService.Messaging;
using HarborManagementService.Models;
using Microsoft.AspNetCore.Mvc;

namespace HarborManagementService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShipController : ControllerBase
    {
        private readonly HarborContext _context;

        public ShipController(HarborContext context)
        {
            _context = context;
        }

        [HttpGet]
        public ActionResult<ApiResponse<IEnumerable<Ship>>> GetAll()
        {
            var ships = _context.Ships.ToList();
            return Ok(new ApiResponse<IEnumerable<Ship>>("Ships retrieved", 200, ships));
        }

        [HttpPost]
        public ActionResult<ApiResponse<Ship>> Create(Ship ship)
        {
            ship.Status = "Planned";
            _context.Ships.Add(ship);
            _context.SaveChanges();

            var publisher = new EventPublisher();
            publisher.Publish(new
            {
                EventType = "ShipPlanned",
                ShipId = ship.Id,
                Name = ship.Name,
                Company = ship.CompanyName,
                ArrivalTime = ship.ArrivalTime,
                DepartureTime = ship.DepartureTime,
                NeedsService = ship.NeedsService,
                Status = ship.Status
            });

            return CreatedAtAction(nameof(GetAll), new { id = ship.Id },
                new ApiResponse<Ship>("Ship created", 201, ship));
        }

        [HttpPost("arrive/{id}")]
        public IActionResult MarkShipAsArrived(int id)
        {
            var ship = _context.Ships.Find(id);
            if (ship == null)
                return NotFound(new ApiResponse<string>("Ship not found", 404));

            ship.ArrivalTime = DateTime.UtcNow;
            ship.Status = "Arrived";
            _context.SaveChanges();

            var publisher = new EventPublisher();
            publisher.Publish(new
            {
                EventType = "ShipArrived",
                ShipId = ship.Id,
                Name = ship.Name,
                ArrivalTime = ship.ArrivalTime,
                Company = ship.CompanyName,
                NeedsService = ship.NeedsService,
                status = ship.Status
            });

            return Ok(new ApiResponse<Ship>("Ship marked as arrived", 200, ship));
        }

        [HttpPost("depart/{id}")]
        public IActionResult MarkShipAsDeparted(int id)
        {
            var ship = _context.Ships.Find(id);
            if (ship == null)
                return NotFound(new ApiResponse<string>("Ship not found", 404));

            ship.DepartureTime = DateTime.UtcNow;
            ship.Status = "Departed";
            _context.SaveChanges();

            var publisher = new EventPublisher();
            publisher.Publish(new
            {
                EventType = "ShipDeparted",
                ShipId = ship.Id,
                Name = ship.Name,
                DepartureTime = ship.DepartureTime,
                status = ship.Status
            });

            return Ok(new ApiResponse<Ship>("Ship marked as departed", 200, ship));
        }
    }

}
