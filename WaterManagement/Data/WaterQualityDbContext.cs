using Microsoft.EntityFrameworkCore;
using WaterManagement.Models;

namespace WaterManagement.Data
{
    public class WaterQualityDbContext : DbContext
    {
        public WaterQualityDbContext(DbContextOptions<WaterQualityDbContext> options) : base(options) { }

        public DbSet<WaterQuality> WaterQualities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WaterQuality>()
                .HasIndex(w => w.Timestamp);
        }
    }
}
