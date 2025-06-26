namespace Billing.Commands
{
    public class MakePaymentCommand
    {
        public string ShippingCompanyName { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaidAt { get; set; }
    }
}