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
        private readonly bool _isTestMode;
        private readonly TimeSpan _testInterval = TimeSpan.FromSeconds(30);

        public MonthlyInvoiceService(IServiceProvider serviceProvider, ILogger<MonthlyInvoiceService> logger, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _isTestMode = configuration.GetValue<bool>("InvoiceService:TestMode", true);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"Invoice service starting in {(_isTestMode ? "TEST" : "PRODUCTION")} mode");

            if (_isTestMode)
            {
                await RunTestMode(stoppingToken);
            }
            else
            {
                await RunProductionMode(stoppingToken);
            }
        }

        private async Task RunTestMode(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"Running in test mode - generating invoices every {_testInterval.TotalSeconds} seconds");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await GenerateInvoicesForCurrentMonth();
                    await Task.Delay(_testInterval, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in test mode invoice generation");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }

        private async Task RunProductionMode(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.UtcNow;
                    var nextRun = GetNextMonthlyInvoiceDate(now);
                    var delay = nextRun - now;

                    _logger.LogInformation($"Next invoice generation scheduled for: {nextRun:yyyy-MM-dd HH:mm:ss} UTC");
                    await Task.Delay(delay, stoppingToken);

                    // Generate invoices for the previous month
                    var previousMonth = now.AddMonths(-1);
                    await GenerateInvoicesForMonth(previousMonth.Year, previousMonth.Month);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in production invoice generation");
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }

        private DateTime GetNextMonthlyInvoiceDate(DateTime now)
        {
            // Run on the 1st of each month at 02:00 UTC
            var nextMonth = now.AddMonths(1);
            return new DateTime(nextMonth.Year, nextMonth.Month, 1, 2, 0, 0, DateTimeKind.Utc);
        }

        private async Task GenerateInvoicesForCurrentMonth()
        {
            var now = DateTime.UtcNow;
            var currentMonth = new DateTime(now.Year, now.Month, 1);
            await GenerateInvoicesForMonth(currentMonth.Year, currentMonth.Month);
        }

        private async Task GenerateInvoicesForMonth(int year, int month)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PortDbContext>();

            var invoiceMonth = new DateTime(year, month, 1);
            var invoiceEnd = invoiceMonth.AddMonths(1).AddDays(-1);
            var invoiceKey = $"{year:0000}-{month:00}";

            _logger.LogInformation($"Generating invoices for {invoiceKey}");

            var rentedDocks = await GetRentedDocksForMonth(context, invoiceMonth, invoiceEnd);
            _logger.LogInformation($"Found {rentedDocks.Count} rented docks for {invoiceKey}");

            using var publisher = new EventPublisher();
            var invoicesGenerated = 0;

            foreach (var dock in rentedDocks)
            {
                if (dock.ShipmentCompany == null)
                {
                    _logger.LogWarning($"Dock {dock.Name} has no associated shipment company, skipping");
                    continue;
                }

                var invoice = await CreateOrGetInvoice(context, dock, invoiceMonth, invoiceEnd, invoiceKey);
                if (invoice != null)
                {
                    await PublishInvoiceEvent(publisher, dock, invoice);
                    invoicesGenerated++;

                    _logger.LogInformation($"Processed invoice for {dock.ShipmentCompany.CompanyName} - Dock {dock.Name}: ${invoice.Amount:F2}");
                }
            }

            _logger.LogInformation($"Completed invoice generation for {invoiceKey}. Generated: {invoicesGenerated} invoices");
        }

        private async Task<List<Dock>> GetRentedDocksForMonth(PortDbContext context, DateTime monthStart, DateTime monthEnd)
        {
            return await context.Docks
                .Include(d => d.ShipmentCompany)
                .Where(d => d.IsRented &&
                           d.RentalStart.HasValue &&
                           d.RentalStart <= monthEnd &&
                           (d.RentalEnd == null || d.RentalEnd >= monthStart))
                .ToListAsync();
        }

        private async Task<Invoice?> CreateOrGetInvoice(PortDbContext context, Dock dock, DateTime monthStart, DateTime monthEnd, string invoiceKey)
        {
            // Check if invoice already exists
            var existingInvoice = await context.Invoices
                .FirstOrDefaultAsync(i => i.DockId == dock.Id && i.InvoiceMonth == invoiceKey);

            if (existingInvoice != null)
            {
                _logger.LogDebug($"Invoice already exists for dock {dock.Name} in {invoiceKey}");
                return _isTestMode ? existingInvoice : null; // In test mode, republish existing invoices
            }

            // Calculate rental period and amount
            var rentalPeriod = CalculateRentalPeriod(dock, monthStart, monthEnd);
            var amount = Math.Round(rentalPeriod.Days * dock.PricePerDay, 2);

            var invoice = new Invoice
            {
                DockId = dock.Id,
                ShipmentCompanyId = dock.ShipmentCompany!.Id,
                InvoiceMonth = invoiceKey,
                RentalStart = rentalPeriod.Start,
                RentalEnd = rentalPeriod.End,
                DaysRented = rentalPeriod.Days,
                PricePerDay = dock.PricePerDay,
                Amount = (decimal)amount,
                GeneratedAt = DateTime.UtcNow,
            };

            context.Invoices.Add(invoice);
            await context.SaveChangesAsync();

            _logger.LogInformation($"Created new invoice with ID: {invoice.Id}");
            return invoice;
        }

        private (DateTime Start, DateTime End, int Days) CalculateRentalPeriod(Dock dock, DateTime monthStart, DateTime monthEnd)
        {
            var actualStart = dock.RentalStart!.Value > monthStart ? dock.RentalStart.Value : monthStart;
            var actualEnd = dock.RentalEnd.HasValue && dock.RentalEnd.Value < monthEnd ? dock.RentalEnd.Value : monthEnd;
            var days = (actualEnd - actualStart).Days + 1;

            return (actualStart, actualEnd, days);
        }

        private async Task PublishInvoiceEvent(EventPublisher publisher, Dock dock, Invoice invoice)
        {
            var invoiceEvent = new
            {
                EventType = "MonthlyDockInvoice",
                InvoiceId = invoice.Id,
                ShipmentCompany = dock.ShipmentCompany!.CompanyName,
                ShipmentCompanyEmail = dock.ShipmentCompany.CompanyEmail,
                ShipmentCompanyPhone = dock.ShipmentCompany.CompanyPhone,
                ShipmentCompanyCardNumber = dock.ShipmentCompany.CardNumber,
                DockName = dock.Name,
                RentalStart = invoice.RentalStart,
                RentalEnd = invoice.RentalEnd,
                DaysRented = invoice.DaysRented,
                PricePerDay = invoice.PricePerDay,
                Amount = (double)invoice.Amount,
                InvoiceMonth = invoice.InvoiceMonth,
                GeneratedAt = invoice.GeneratedAt,
                IsTestMode = _isTestMode
            };

            try
            {
                publisher.Publish(invoiceEvent);
                _logger.LogInformation($"Successfully published invoice event for invoice {invoice.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to publish invoice event for invoice {invoice.Id}");
                throw;
            }
        }
    }
}