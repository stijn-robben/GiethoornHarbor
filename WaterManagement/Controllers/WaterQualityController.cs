using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WaterManagement.Data;
using WaterManagement.Dto;
using WaterManagement.Models;
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
        public async Task<ActionResult<CurrentWaterQualityDto>> GetCurrent()
        {
            var current = await _waterQualityService.GetCurrentWaterQualityDtoAsync();
            return Ok(current);
        }

        // GET: api/waterquality/history (optioneel voor frontend)
        [HttpGet("history")]
        public async Task<ActionResult<List<WaterQuality>>> GetHistory([FromQuery] int hours = 24)
        {
            var since = DateTime.UtcNow.AddHours(-hours);

            using var context = new WaterQualityDbContext(new DbContextOptionsBuilder<WaterQualityDbContext>().Options);
            var history = await context.WaterQualities
                .Where(w => w.Timestamp >= since)
                .OrderByDescending(w => w.Timestamp)
                .ToListAsync();

            return Ok(history);
        }
    }
}
