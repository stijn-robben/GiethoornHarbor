namespace Billing.Models
{
    public class Invoice
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string CompanyName { get; set; }
        public DateTime ArrivalTime { get; set; }
        public DateTime DepartureTime { get; set; }
        public bool NeedsService { get; set; }
    }
}
