using Dock_ShipmentCompany.Database;
using Dock_ShipmentCompany.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Dock_ShipmentCompany.Services
{
    public class DatabaseSeeder
    {
        private readonly PortDbContext _context;
        private readonly ILogger<DatabaseSeeder> _logger;

        public DatabaseSeeder(PortDbContext context, ILogger<DatabaseSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                // Check if docks already exist
                if (_context.Docks.Any())
                {
                    _logger.LogInformation("Database already contains dock data. Skipping seed.");
                    return;
                }

                _logger.LogInformation("Starting database seeding...");

                // Read the JSON file
                var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "docks-seed.json");

                if (!File.Exists(jsonPath))
                {
                    _logger.LogWarning("Seed file not found at: {JsonPath}", jsonPath);
                    return;
                }

                var jsonContent = await File.ReadAllTextAsync(jsonPath);
                var docksData = JsonSerializer.Deserialize<List<DockSeedData>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (docksData == null || !docksData.Any())
                {
                    _logger.LogWarning("No dock data found in seed file.");
                    return;
                }

                // Convert seed data to Dock entities (without Id to let EF generate them)
                var docks = docksData.Select(d => new Dock
                {
                    // Don't set Id - let EF generate it
                    Name = d.Name,
                    IsOccupied = d.IsOccupied,
                    IsRented = d.IsRented,
                    PricePerDay = d.PricePerDay,
                    RentalStart = d.RentalStart,
                    RentalEnd = d.RentalEnd,
                    ShipmentCompany = d.ShipmentCompany
                }).ToList();

                // Add docks to database
                await _context.Docks.AddRangeAsync(docks);
                await _context.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }
    }

    // Helper class for deserializing JSON data
    public class DockSeedData
    {
        public int Id { get; set; }

        public required string Name { get; set; } //A1
        public bool IsOccupied { get; set; }
        public bool IsRented { get; set; }
        public DateTime? RentalStart { get; set; }
        public DateTime? RentalEnd { get; set; }  // Null = indefinite
        public ShipmentCompany? ShipmentCompany { get; set; }
        public double PricePerDay { get; set; } // Price per day in USD
    }
}