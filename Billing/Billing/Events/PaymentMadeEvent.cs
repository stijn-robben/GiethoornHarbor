namespace Billing.Events
{
    public class PaymentMadeEvent
    {
        public string ShippingCompanyName { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaidAt { get; set; }
    }
}