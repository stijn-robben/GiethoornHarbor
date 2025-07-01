using Microsoft.EntityFrameworkCore;
using WaterManagement.Data;
using WaterManagement.Dto;
using WaterManagement.Models;

namespace WaterManagement.Services
{
    public class WaterQualityService
    {
        private readonly WaterQualityDbContext _context;
        private const int BASE_QUALITY = 80;
        private const int QUALITY_IMPACT_PER_SHIP = 5;
        private int _currentShipCount = 0;

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
            Console.WriteLine($"Water quality updated after ship arrival");
        }

        public async Task HandleShipDepartedAsync()
        {
            if (_currentShipCount > 0)
                _currentShipCount--;
            await UpdateWaterQualityAsync();
            Console.WriteLine($"Water quality updated after ship departure");
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
            Console.WriteLine($"New water quality record saved to database");
        }

        private int CalculateQualityScore(int shipsInHarbor)
        {
            var score = BASE_QUALITY - (shipsInHarbor * QUALITY_IMPACT_PER_SHIP);
            Console.WriteLine($"Quality calculation Score");
            return Math.Clamp(score, 0, 100);
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
                return new CurrentWaterQualityDto
                {
                    LastUpdated = DateTime.UtcNow,
                    QualityScore = BASE_QUALITY,
                    Level = WaterQualityLevel.Excellent,
                    Status = GetStatusText(WaterQualityLevel.Excellent)
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
