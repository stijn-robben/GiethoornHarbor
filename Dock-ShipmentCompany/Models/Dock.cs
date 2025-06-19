namespace Dock_ShipmentCompany.Models
{
    public class Dock
    {
        public int Id { get; set; }

        public required string Name { get; set; } //A1
        public bool IsOccupied { get; set; }
        public bool IsRented { get; set; }
        public DateTime? RentalStart { get; set; }
        public DateTime? RentalEnd { get; set; }  // Null = indefinite
        public ShipmentCompany? ShipmentCompany { get; set; }
        public double PricePerDay { get; set; } // Price per day in USD
    }
}
