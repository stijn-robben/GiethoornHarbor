using WaterManagement.Models;

namespace WaterManagement.Dto
{
    public class CurrentWaterQualityDto
    {
        public DateTime LastUpdated { get; set; }
        public int QualityScore { get; set; }
        public WaterQualityLevel Level { get; set; }
        public string Status { get; set; }
    }
}
