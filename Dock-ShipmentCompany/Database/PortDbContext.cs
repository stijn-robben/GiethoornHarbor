using Dock_ShipmentCompany.Models;
using Microsoft.EntityFrameworkCore;

namespace Dock_ShipmentCompany.Database
{
    public class PortDbContext : DbContext
    {
        public PortDbContext(DbContextOptions<PortDbContext> options) : base(options) { }

        public DbSet<Dock> Docks { get; set; }
        public DbSet<ShipmentCompany> ShipmentCompanies { get; set; }
    }

}
