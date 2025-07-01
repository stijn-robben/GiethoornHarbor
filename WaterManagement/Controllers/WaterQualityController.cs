using Microsoft.AspNetCore.Mvc;
using WaterManagement.Dto;
using WaterManagement.Services;

namespace WaterManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WaterQualityController : ControllerBase
    {
        private readonly WaterQualityService _waterQualityService;

        public WaterQualityController(WaterQualityService waterQualityService)
        {
            _waterQualityService = waterQualityService;
        }

        // GET: api/waterquality/current
        [HttpGet("current")]
        public async Task<ActionResult<CurrentWaterQualityDto>> GetCurrentAsync()
        {
            var current = await _waterQualityService.GetCurrentWaterQualityDtoAsync();
            return Ok(current);
        }

        // POST: api/waterquality/simulate-ship-arrived
        [HttpPost("simulate-ship-arrived")]
        public async Task<IActionResult> SimulateShipArrivedAsync()
        {
            await _waterQualityService.HandleShipArrivedAsync();
            return Ok("Ship arrival simulated - water quality should decrease");
        }

        // POST: api/waterquality/simulate-ship-departed
        [HttpPost("simulate-ship-departed")]
        public async Task<IActionResult> SimulateShipDepartedAsync()
        {
            await _waterQualityService.HandleShipDepartedAsync();
            return Ok("Ship departure simulated - water quality should improve");
        }
    }
}