namespace HarborManagementService.Models
{
    public class Ship
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public DateTime ArrivalTime { get; set; }
        public DateTime DepartureTime { get; set; }
        public bool NeedsService { get; set; }
        public string Status { get; set; } = "Planned";
        public string ArrivalTimeFormatted => ArrivalTime.ToString("yyyy-MM-dd HH:mm");
        public string DepartureTimeFormatted => DepartureTime.ToString("yyyy-MM-dd HH:mm");
    }
}
