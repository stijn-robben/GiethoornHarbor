namespace Billing.Models
{
    public class ShippingCompany
    {
        public int Id { get; set; }
        public string CompanyName { get; set; }
        public string ContactEmail { get; set; }

        public string ShipmentCompanyCardNumber { get; set; }
    }
}
