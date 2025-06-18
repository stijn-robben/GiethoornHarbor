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
        public ActionResult<IEnumerable<Ship>> GetAll()
        {
            return Ok(_context.Ships.ToList());
        }

        [HttpPost]
        public ActionResult<Ship> Create(Ship ship)
        {
            _context.Ships.Add(ship);
            _context.SaveChanges();
            var publisher = new EventPublisher();
            publisher.Publish(new
            {
                EventType = "ShipCreated",
                ShipId = ship.Id,
                Name = ship.Name,
                Company = ship.CompanyName,
                ArrivalTime = ship.ArrivalTime,
                DepartureTime = ship.DepartureTime,
                NeedsService = ship.NeedsService
            });
            return CreatedAtAction(nameof(GetAll), new { id = ship.Id }, ship);
        }
        [HttpPost("arrive/{id}")]
        public IActionResult MarkShipAsArrived(int id)
        {
            var ship = _context.Ships.Find(id);
            if (ship == null) return NotFound();

            var publisher = new EventPublisher();
            publisher.Publish(new
            {
                EventType = "ShipArrived",
                ShipId = ship.Id,
                Name = ship.Name,
                ArrivalTime = ship.ArrivalTime
            });

            return Ok(new { message = "ShipArrived event published." });
        }
        [HttpPost("depart/{id}")]
        public IActionResult MarkShipAsDeparted(int id)
        {
            var ship = _context.Ships.Find(id);
            if (ship == null) return NotFound();

            var publisher = new EventPublisher();
            publisher.Publish(new
            {
                EventType = "ShipDeparted",
                ShipId = ship.Id,
                Name = ship.Name,
                DepartureTime = ship.DepartureTime
            });

            return Ok(new { message = "ShipDeparted event published." });
        }


    }

}
