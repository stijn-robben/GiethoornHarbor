using Microsoft.EntityFrameworkCore;
using WaterManagement.Data;
using WaterManagement.Dto;
using WaterManagement.Models;

namespace WaterManagement.Services
{
    public class WaterQualityService
    {
        private readonly WaterQualityDbContext _context;
        private const int BASE_QUALITY = 80; // Basis kwaliteit zonder schepen
        private const int QUALITY_IMPACT_PER_SHIP = 5; // Hoeveel slechter per schip
        private int _currentShipCount = 0; // Houdt bij hoeveel schepen er zijn

        public WaterQualityService(WaterQualityDbContext context)
        {
            _context = context;
        }

        public async Task<WaterQuality> GetCurrentWaterQualityAsync()
        {
            return await _context.WaterQualities
                .OrderByDescending(w => w.Timestamp)
                .FirstOrDefaultAsync();
        }

        public async Task HandleShipArrivedAsync()
        {
            _currentShipCount++;
            await UpdateWaterQualityAsync();
        }

        public async Task HandleShipDepartedAsync()
        {
            _currentShipCount = Math.Max(0, _currentShipCount - 1);
            await UpdateWaterQualityAsync();
        }

        private async Task UpdateWaterQualityAsync()
        {
            var qualityScore = CalculateQualityScore(_currentShipCount);
            var level = GetQualityLevel(qualityScore);

            var waterQuality = new WaterQuality
            {
                Timestamp = DateTime.UtcNow,
                QualityScore = qualityScore,
                Level = level
            };

            _context.WaterQualities.Add(waterQuality);
            await _context.SaveChangesAsync();
        }

        private int CalculateQualityScore(int shipsInHarbor)
        {
            var score = BASE_QUALITY - (shipsInHarbor * QUALITY_IMPACT_PER_SHIP);
            return Math.Max(0, Math.Min(100, score));
        }

        private WaterQualityLevel GetQualityLevel(int score)
        {
            return score switch
            {
                >= 80 => WaterQualityLevel.Excellent,
                >= 60 => WaterQualityLevel.Good,
                >= 40 => WaterQualityLevel.Fair,
                >= 20 => WaterQualityLevel.Poor,
                _ => WaterQualityLevel.Critical
            };
        }

        private string GetStatusText(WaterQualityLevel level)
        {
            return level switch
            {
                WaterQualityLevel.Excellent => "Uitstekend",
                WaterQualityLevel.Good => "Goed",
                WaterQualityLevel.Fair => "Redelijk",
                WaterQualityLevel.Poor => "Slecht",
                WaterQualityLevel.Critical => "Kritiek",
                _ => "Onbekend"
            };
        }

        public async Task<CurrentWaterQualityDto> GetCurrentWaterQualityDtoAsync()
        {
            var current = await GetCurrentWaterQualityAsync();

            if (current == null)
            {
                // Default waarde als er nog geen metingen zijn
                return new CurrentWaterQualityDto
                {
                    LastUpdated = DateTime.UtcNow,
                    QualityScore = BASE_QUALITY,
                    Level = WaterQualityLevel.Excellent,
                    Status = "Uitstekend"
                };
            }

            return new CurrentWaterQualityDto
            {
                LastUpdated = current.Timestamp,
                QualityScore = current.QualityScore,
                Level = current.Level,
                Status = GetStatusText(current.Level)
            };
        }
    }
}
