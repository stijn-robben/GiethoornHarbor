using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Billing.Data
{
    public class HarborContextFactory : IDesignTimeDbContextFactory<BillingContext>
    {
        public BillingContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<BillingContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Ensure the Microsoft.EntityFrameworkCore.SqlServer package is installed
            optionsBuilder.UseSqlServer(connectionString);

            return new BillingContext(optionsBuilder.Options);
        }
    }
}
