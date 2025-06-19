using Dock_ShipmentCompany.Models;
using Microsoft.EntityFrameworkCore;

namespace Dock_ShipmentCompany.Database
{
    public class PortDbContext : DbContext
    {
        public PortDbContext(DbContextOptions<PortDbContext> options) : base(options) { }

        public DbSet<Dock> Docks { get; set; }
        public DbSet<ShipmentCompany> ShipmentCompanies { get; set; }
        public DbSet<Invoice> Invoices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Invoice>()
    .HasOne(i => i.Dock)
    .WithMany()
    .HasForeignKey(i => i.DockId)
    .OnDelete(DeleteBehavior.Restrict); 

            // Invoice → ShipmentCompany
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.ShipmentCompany)
                .WithMany()
                .HasForeignKey(i => i.ShipmentCompanyId)
                .OnDelete(DeleteBehavior.Restrict); 

            // Unique constraint for dock + month
            modelBuilder.Entity<Invoice>()
                .HasIndex(i => new { i.DockId, i.InvoiceMonth })
                .IsUnique();

            // Decimal precision
            modelBuilder.Entity<Invoice>()
                .Property(i => i.Amount)
                .HasPrecision(10, 2);

            // Make Dock.Name unique
            modelBuilder.Entity<Dock>()
                .HasIndex(d => d.Name)
                .IsUnique();

            // Make ShipmentCompany.CompanyName unique
            modelBuilder.Entity<ShipmentCompany>()
                .HasIndex(sc => sc.CompanyName)
                .IsUnique();

        }
    }
}