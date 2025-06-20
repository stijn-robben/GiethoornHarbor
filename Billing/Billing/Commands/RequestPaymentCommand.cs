namespace Billing.Commands
{
    public class RequestPaymentCommand
    {
        public string ShippingCompanyName { get; set; }
        public decimal Amount { get; set; }
        public DateTime RequestedAt { get; set; }
    }
}