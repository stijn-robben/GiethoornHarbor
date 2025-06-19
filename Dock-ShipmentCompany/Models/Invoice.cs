namespace Dock_ShipmentCompany.Models
{
    public class Invoice
    {
        public int Id { get; set; }
        public int DockId { get; set; }
        public Dock Dock { get; set; }
        public int ShipmentCompanyId { get; set; }
        public ShipmentCompany ShipmentCompany { get; set; }
        public  string InvoiceMonth { get; set; } // Format: "2025-06"
        public DateTime RentalStart { get; set; }
        public DateTime RentalEnd { get; set; }
        public int DaysRented { get; set; }
        public double PricePerDay { get; set; }
        public decimal Amount { get; set; }
        public DateTime GeneratedAt { get; set; }
        public bool IsPaid { get; set; } = false;
    }

}
