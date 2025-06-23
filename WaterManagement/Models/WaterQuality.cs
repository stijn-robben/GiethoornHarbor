namespace WaterManagement.Models
{
    public class WaterQuality
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } // Wanneer deze meting is gedaan
        public int QualityScore { get; set; } // 0-100
        public WaterQualityLevel Level { get; set; }
    }
}
