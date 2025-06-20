namespace Billing.Events
{
    public class PaymentRequestedEvent
    {
        public string ShippingCompanyName { get; set; }
        public decimal Amount { get; set; }
        public DateTime RequestedAt { get; set; }
    }
}