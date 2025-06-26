using Dock_ShipmentCompany.Database;
using Dock_ShipmentCompany.Models;
using Dock_ShipmentCompany.Messaging;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Dock_ShipmentCompany.Services
{
    public class CompanySyncService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CompanySyncService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _csvUrl;

        public CompanySyncService(
            IServiceProvider serviceProvider,
            ILogger<CompanySyncService> logger,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _csvUrl = configuration["CompanySync:CsvUrl"] ?? throw new InvalidOperationException("CompanySync:CsvUrl configuration is required");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Company Sync Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SyncCompaniesAsync();
                    _logger.LogInformation("Company sync completed successfully at {Time}", DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during company synchronization");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task SyncCompaniesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PortDbContext>();
            var eventPublisher = scope.ServiceProvider.GetRequiredService<EventPublisher>();

            try
            {
                var csvData = await FetchCsvDataAsync();
                if (string.IsNullOrEmpty(csvData))
                {
                    _logger.LogWarning("No CSV data received from URL: {Url}", _csvUrl);
                    return;
                }

                var companies = ParseCsvData(csvData);
                if (!companies.Any())
                {
                    _logger.LogWarning("No companies parsed from CSV data");
                    return;
                }

                _logger.LogInformation("Parsed {Count} companies from CSV", companies.Count);
                await SyncWithDatabaseAsync(context, eventPublisher, companies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during company synchronization");
                throw;
            }
        }

        private async Task<string> FetchCsvDataAsync()
        {
            try
            {
                _logger.LogDebug("Fetching CSV data from: {Url}", _csvUrl);
                using var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.GetAsync(_csvUrl);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while fetching CSV data from {Url}", _csvUrl);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching CSV data");
                throw;
            }
        }

        private List<CompanyCsvRecord> ParseCsvData(string csvData)
        {
            var rawCompanies = new List<CompanyCsvRecord>();
            var lines = csvData.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 1; i < lines.Length; i++) // Skip header
            {
                try
                {
                    var company = ParseCsvLine(lines[i]);
                    if (company != null)
                    {
                        rawCompanies.Add(company);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error parsing CSV line {LineNumber}: {Line}", i + 1, lines[i]);
                }
            }

            // Deduplicate by CompanyName (case-insensitive), keep first occurrence
            var uniqueCompanies = rawCompanies
                .GroupBy(c => c.CompanyName.ToUpperInvariant())
                .Select(g => g.First())
                .ToList();

            return uniqueCompanies;
        }


        private CompanyCsvRecord? ParseCsvLine(string line)
        {
            var fields = ParseCsvFields(line);

            if (fields.Count < 5)
            {
                _logger.LogWarning("CSV line has insufficient fields: {Line}", line);
                return null;
            }

            var fullAddress = fields[4].Trim('"');
            var (addressLine, city) = ParseAddress(fullAddress);

            return new CompanyCsvRecord
            {
                CompanyName = fields[0].Trim(),
                FirstName = fields[1].Trim(),
                LastName = fields[2].Trim(),
                PhoneNumber = fields[3].Trim(),
                AddressLine = addressLine,
                City = city
            };
        }

        private List<string> ParseCsvFields(string line)
        {
            var fields = new List<string>();
            var currentField = "";
            var inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    currentField += c;
                }
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(currentField);
                    currentField = "";
                }
                else
                {
                    currentField += c;
                }
            }

            if (!string.IsNullOrEmpty(currentField))
            {
                fields.Add(currentField);
            }

            return fields;
        }

        private (string addressLine, string city) ParseAddress(string fullAddress)
        {
            var parts = fullAddress.Split(',');
            if (parts.Length >= 2)
            {
                var addressPart = parts[0].Trim();
                var cityPart = parts[1].Trim();
                var cityWords = cityPart.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var city = cityWords.Length > 1 ? string.Join(" ", cityWords.Skip(1)) : cityPart;
                return (addressPart, city);
            }

            return (fullAddress, "Unknown");
        }

        private async Task SyncWithDatabaseAsync(PortDbContext context, EventPublisher eventPublisher, List<CompanyCsvRecord> csvCompanies)
        {
            var existingCompanies = await context.ShipmentCompanies
                .AsNoTracking()
                .ToListAsync();

            var newCompanies = new List<ShipmentCompany>();
            var updatedCompanies = new List<ShipmentCompany>();

            foreach (var csvCompany in csvCompanies)
            {
                var existingCompany = existingCompanies.FirstOrDefault(c =>
                    c.CompanyName.Equals(csvCompany.CompanyName, StringComparison.OrdinalIgnoreCase));

                if (existingCompany == null)
                {
                    var newCompany = new ShipmentCompany
                    {
                        CompanyName = csvCompany.CompanyName,
                        AddressLine = csvCompany.AddressLine,
                        City = csvCompany.City,
                        Country = "Netherlands",
                        CardNumber = GenerateCardNumber(),
                        CompanyPhone = csvCompany.PhoneNumber,
                        CompanyEmail = GenerateCompanyEmail(csvCompany.CompanyName)
                    };
                    newCompanies.Add(newCompany);
                    _logger.LogDebug("New company found: {CompanyName}", csvCompany.CompanyName);
                }
                else
                {
                    var updatedCompany = new ShipmentCompany
                    {
                        Id = existingCompany.Id,
                        CompanyName = csvCompany.CompanyName,
                        AddressLine = csvCompany.AddressLine,
                        City = csvCompany.City,
                        Country = "Netherlands",
                        CardNumber = existingCompany.CardNumber,
                        CompanyPhone = csvCompany.PhoneNumber,
                        CompanyEmail = existingCompany.CompanyEmail
                    };

                    if (HasCompanyChanged(existingCompany, updatedCompany))
                    {
                        updatedCompanies.Add(updatedCompany);
                        _logger.LogDebug("Company needs update: {CompanyName}", csvCompany.CompanyName);
                    }
                }
            }

            if (newCompanies.Any())
            {
                _logger.LogInformation("Adding {Count} new companies", newCompanies.Count);
                await context.ShipmentCompanies.AddRangeAsync(newCompanies);
            }

            if (updatedCompanies.Any())
            {
                _logger.LogInformation("Updating {Count} companies", updatedCompanies.Count);
                foreach (var updatedCompany in updatedCompanies)
                {
                    context.ShipmentCompanies.Update(updatedCompany);
                    _logger.LogDebug("Updating company: {CompanyName}", updatedCompany.CompanyName);
                }
            }

            if (newCompanies.Any() || updatedCompanies.Any())
            {
                try
                {
                    await context.SaveChangesAsync();
                    _logger.LogInformation("Successfully synced {NewCount} new and {UpdatedCount} updated companies",
                        newCompanies.Count, updatedCompanies.Count);

                    foreach (var company in newCompanies)
                    {
                        await PublishShipmentCompanyCreatedEvent(eventPublisher, company);
                    }
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("duplicate key") == true)
                {
                    _logger.LogError(ex, "Duplicate key error during company sync. This might indicate a race condition or data inconsistency.");
                    await RetryWithIndividualOperationsAsync(context, eventPublisher, newCompanies, updatedCompanies);
                }
            }
            else
            {
                _logger.LogInformation("No changes detected in company data");
            }
        }

        private async Task RetryWithIndividualOperationsAsync(PortDbContext context, EventPublisher eventPublisher,
            List<ShipmentCompany> newCompanies, List<ShipmentCompany> updatedCompanies)
        {
            _logger.LogInformation("Retrying sync with individual operations to handle potential race conditions");
            context.ChangeTracker.Clear();

            foreach (var newCompany in newCompanies)
            {
                try
                {
                    var existsNow = await context.ShipmentCompanies
                        .AnyAsync(c => c.CompanyName.ToUpper() == newCompany.CompanyName.ToUpper());

                    if (!existsNow)
                    {
                        context.ShipmentCompanies.Add(newCompany);
                        await context.SaveChangesAsync();
                        await PublishShipmentCompanyCreatedEvent(eventPublisher, newCompany);
                        _logger.LogInformation("Successfully added company: {CompanyName}", newCompany.CompanyName);
                    }
                    else
                    {
                        _logger.LogInformation("Company {CompanyName} was already created by another process", newCompany.CompanyName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to add company: {CompanyName}", newCompany.CompanyName);
                }
                finally
                {
                    context.ChangeTracker.Clear();
                }
            }

            foreach (var updatedCompany in updatedCompanies)
            {
                try
                {
                    var existingCompany = await context.ShipmentCompanies
                        .FirstOrDefaultAsync(c => c.Id == updatedCompany.Id);

                    if (existingCompany != null)
                    {
                        existingCompany.AddressLine = updatedCompany.AddressLine;
                        existingCompany.City = updatedCompany.City;
                        existingCompany.Country = updatedCompany.Country;
                        existingCompany.CompanyPhone = updatedCompany.CompanyPhone;

                        await context.SaveChangesAsync();
                        _logger.LogInformation("Successfully updated company: {CompanyName}", updatedCompany.CompanyName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update company: {CompanyName}", updatedCompany.CompanyName);
                }
                finally
                {
                    context.ChangeTracker.Clear();
                }
            }
        }

        private bool HasCompanyChanged(ShipmentCompany existing, ShipmentCompany updated)
        {
            return existing.AddressLine != updated.AddressLine ||
                   existing.City != updated.City ||
                   existing.Country != updated.Country ||
                   existing.CompanyPhone != updated.CompanyPhone ||
                   existing.CompanyEmail != updated.CompanyEmail;
        }

        private string GenerateCardNumber()
        {
            var random = new Random();
            return $"4{random.Next(100, 999)}{random.Next(1000, 9999)}{random.Next(1000, 9999)}{random.Next(1000, 9999)}";
        }

        private string GenerateCompanyEmail(string companyName)
        {
            var cleanName = companyName.ToLower().Replace(" ", "").Replace("&", "and");
            return $"contact@{cleanName}.com";
        }

        private async Task PublishShipmentCompanyCreatedEvent(EventPublisher eventPublisher, ShipmentCompany company)
        {
            var shipmentCompanyCreatedEvent = new
            {
                EventType = "ShipmentCompanyCreated",
                Timestamp = DateTime.UtcNow,
                Data = new
                {
                    Id = company.Id,
                    CompanyName = company.CompanyName,
                    AddressLine = company.AddressLine,
                    City = company.City,
                    Country = company.Country,
                    CardNumber = company.CardNumber,
                    CompanyPhone = company.CompanyPhone,
                    CompanyEmail = company.CompanyEmail,
                    Docks = company.Docks
                }
            };

            eventPublisher.Publish(shipmentCompanyCreatedEvent);
        }
    }

    public class CompanyCsvRecord
    {
        public string CompanyName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string AddressLine { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }
}
