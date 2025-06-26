using Billing.Data;
using Billing.Events;
using Billing.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

public class PaymentRequestedEventProjector : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public PaymentRequestedEventProjector(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<BillingContext>();

                // Find events that have not been projected yet
                var projectedInvoiceIds = context.Invoices
                    .Select(i => new { i.ShippingCompanyName, i.RequestedAt, i.Amount })
                    .ToList();

                var events = context.StoredEvents
                    .Where(e => e.EventType == nameof(PaymentRequestedEvent))
                    .ToList();

                foreach (var storedEvent in events)
                {
                    var paymentEvent = JsonSerializer.Deserialize<PaymentRequestedEvent>(storedEvent.Data);

                    // Check if invoice already exists for this event (idempotency)
                    bool alreadyProjected = context.Invoices.Any(i =>
                        i.ShippingCompanyName == paymentEvent.ShippingCompanyName &&
                        i.RequestedAt == paymentEvent.RequestedAt &&
                        i.Amount == paymentEvent.Amount);

                    if (!alreadyProjected)
                    {
                        var invoice = new Invoice
                        {
                            ShippingCompanyName = paymentEvent.ShippingCompanyName,
                            Amount = paymentEvent.Amount,
                            RequestedAt = paymentEvent.RequestedAt
                        };
                        context.Invoices.Add(invoice);
                        context.SaveChanges();
                    }
                }
            }

            // Wait before checking for new events again
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}