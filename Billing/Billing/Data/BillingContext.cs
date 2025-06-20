using Billing.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Billing.Data
{
    public class BillingContext : DbContext
    {
        public BillingContext(DbContextOptions<BillingContext> options) : base(options) { }

        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<ShippingCompany> shippingCompanies { get; set; }
        public DbSet<StoredEvent> StoredEvents { get; set; }
    }
}