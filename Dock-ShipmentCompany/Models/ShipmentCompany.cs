namespace Dock_ShipmentCompany.Models
{
    public class ShipmentCompany
    {
        public int Id { get; set; }
        public required string CompanyName { get; set; }
        public required string AddressLine { get; set; }
        public required string City { get; set; }
        public required string Country { get; set; }
        public required string CardNumber { get; set; }
        public required string CompanyPhone { get; set; }
        public required string CompanyEmail { get; set; }
        public ICollection<Dock>? Docks { get; set; }
    }
}
