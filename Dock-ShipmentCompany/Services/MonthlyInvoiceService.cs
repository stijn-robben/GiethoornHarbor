using Dock_ShipmentCompany.Database;
using Dock_ShipmentCompany.Messaging;
using Dock_ShipmentCompany.Models;
using Microsoft.EntityFrameworkCore;

namespace Dock_ShipmentCompany.Services
{
    public class MonthlyInvoiceService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MonthlyInvoiceService> _logger;

        public MonthlyInvoiceService(IServiceProvider serviceProvider, ILogger<MonthlyInvoiceService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Check for missed invoices on startup
            await CheckAndGenerateMissedInvoices();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.UtcNow;
                    var nextRun = GetNextInvoiceDate(now);
                    var delay = nextRun - now;

                    _logger.LogInformation($"Next invoice generation scheduled for: {nextRun:yyyy-MM-dd HH:mm:ss} UTC");

                    // Wait until the next scheduled time
                    await Task.Delay(delay, stoppingToken);

                    // FOR TESTING: Generate invoices for the CURRENT month
                    await GenerateInvoicesForMonth(now.Year, now.Month);

                    // PRODUCTION: Generate invoices for the previous month
                    // var invoiceMonth = now.AddMonths(-1);
                    // await GenerateInvoicesForMonth(invoiceMonth.Year, invoiceMonth.Month);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in monthly invoice service");
                    // Wait 1 hour before retrying
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }

        private DateTime GetNextInvoiceDate(DateTime now)
        {
            // FOR TESTING: Change this to run every 30 seconds
            return now.AddSeconds(30);

            // PRODUCTION: Run on the 1st of each month at 02:00 UTC
            //var nextMonth = now.AddMonths(1);
            //return new DateTime(nextMonth.Year, nextMonth.Month, 1, 2, 0, 0, DateTimeKind.Utc);
        }

        private async Task CheckAndGenerateMissedInvoices()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<PortDbContext>();

                var currentDate = DateTime.UtcNow;
                var currentMonth = new DateTime(currentDate.Year, currentDate.Month, 1);

                // Check for rentals that started before current month and might need invoices
                var oldRentals = await context.Docks
                    .Include(d => d.ShipmentCompany)
                    .Where(d => d.IsRented && d.RentalStart.HasValue && d.RentalStart < currentMonth)
                    .ToListAsync();

                foreach (var dock in oldRentals)
                {
                    var startMonth = new DateTime(dock.RentalStart.Value.Year, dock.RentalStart.Value.Month, 1);
                    var endMonth = dock.RentalEnd.HasValue
                        ? new DateTime(dock.RentalEnd.Value.Year, dock.RentalEnd.Value.Month, 1)
                        : currentMonth.AddMonths(-1); // Up to last month if indefinite

                    // Generate invoices for each missing month
                    for (var month = startMonth; month <= endMonth; month = month.AddMonths(1))
                    {
                        await GenerateInvoicesForMonth(month.Year, month.Month);
                    }
                }

                _logger.LogInformation("Completed check for missed invoices");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for missed invoices");
            }
        }

        private async Task GenerateInvoicesForMonth(int year, int month)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<PortDbContext>();

                // Create one publisher instance for all invoices in this batch
                using var publisher = new EventPublisher();

                var invoiceStart = new DateTime(year, month, 1);
                var invoiceEnd = invoiceStart.AddMonths(1).AddDays(-1);
                var invoiceKey = $"{year:0000}-{month:00}";

                _logger.LogInformation($"Generating invoices for {invoiceKey}");

                // Get all docks that were rented during this month
                var relevantDocks = await context.Docks
                    .Include(d => d.ShipmentCompany)
                    .Where(d => d.IsRented &&
                               d.RentalStart.HasValue &&
                               d.RentalStart <= invoiceEnd &&
                               (d.RentalEnd == null || d.RentalEnd >= invoiceStart))
                    .ToListAsync();

                _logger.LogInformation($"Found {relevantDocks.Count} relevant docks for {invoiceKey}");

                foreach (var dock in relevantDocks)
                {
                    _logger.LogInformation($"Processing dock {dock.Name} for company {dock.ShipmentCompany?.CompanyName}");

                    // Check if ShipmentCompany is null
                    if (dock.ShipmentCompany == null)
                    {
                        _logger.LogWarning($"Dock {dock.Name} has no associated shipment company, skipping invoice generation");
                        continue;
                    }

                    // Check if invoice already exists for this dock and month
                    var existingInvoice = await context.Invoices
                        .FirstOrDefaultAsync(i => i.DockId == dock.Id && i.InvoiceMonth == invoiceKey);

                    if (existingInvoice != null)
                    {
                        _logger.LogDebug($"Invoice already exists for dock {dock.Name} in {invoiceKey}");
                        continue;
                    }

                    // Calculate actual rental period for this month
                    var actualStart = dock.RentalStart.Value > invoiceStart ? dock.RentalStart.Value : invoiceStart;
                    var actualEnd = dock.RentalEnd.HasValue && dock.RentalEnd.Value < invoiceEnd
                        ? dock.RentalEnd.Value
                        : invoiceEnd;

                    var daysRented = (actualEnd - actualStart).Days + 1;
                    var amount = Math.Round(daysRented * dock.PricePerDay, 2);

                    // Create invoice record in database
                    var invoice = new Invoice
                    {
                        DockId = dock.Id,
                        ShipmentCompanyId = dock.ShipmentCompany.Id,
                        InvoiceMonth = invoiceKey,
                        RentalStart = actualStart,
                        RentalEnd = actualEnd,
                        DaysRented = daysRented,
                        PricePerDay = dock.PricePerDay,
                        Amount = (decimal)amount,
                        GeneratedAt = DateTime.UtcNow,
                        IsPaid = false
                    };

                    context.Invoices.Add(invoice);
                    await context.SaveChangesAsync();

                    _logger.LogInformation($"Saved invoice to database with ID: {invoice.Id}");

                    // Publish invoice via RabbitMQ
                    var invoiceEvent = new
                    {
                        EventType = "MonthlyDockInvoice",
                        InvoiceId = invoice.Id,
                        ShipmentCompany = dock.ShipmentCompany.CompanyName,
                        ShipmentCompanyEmail = dock.ShipmentCompany.CompanyEmail,
                        ShipmentCompanyPhone = dock.ShipmentCompany.CompanyPhone,
                        ShipmentCompanyCardNumber = dock.ShipmentCompany.CardNumber,
                        RentalStart = actualStart,
                        RentalEnd = actualEnd,
                        Amount = amount,
                        DockName = dock.Name,
                        PricePerDay = dock.PricePerDay,
                        DaysRented = daysRented,
                        InvoiceMonth = invoiceKey,
                        GeneratedAt = DateTime.UtcNow
                    };

                    try
                    {
                        publisher.Publish(invoiceEvent);
                        _logger.LogInformation($"Successfully published invoice event to RabbitMQ for invoice {invoice.Id}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to publish invoice event to RabbitMQ for invoice {invoice.Id}");
                    }
                    _logger.LogInformation($"Generated invoice for {dock.ShipmentCompany.CompanyName} - Dock {dock.Name} ({invoiceKey}): ${amount:F2}");
                }

                if (relevantDocks.Count == 0)
                {
                    _logger.LogInformation($"No docks found for invoice generation in {invoiceKey}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating invoices for {year}-{month:00}");
                throw;
            }
        }
    }
}