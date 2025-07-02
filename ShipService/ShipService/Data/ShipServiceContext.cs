using Microsoft.EntityFrameworkCore;
using ShipService.Models;

namespace ShipService.Data
{
    public class ShipServiceContext : DbContext
    {
        public ShipServiceContext(DbContextOptions<ShipServiceContext> options) : base(options) { }

        public DbSet<Models.ShipService> ShipServices { get; set; }
        public DbSet<Container> Containers { get; set; }
    }
}
