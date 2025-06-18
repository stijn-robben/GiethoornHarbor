using HarborManagementService.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace HarborManagementService.Data
{
    public class HarborContext : DbContext
    {
        public HarborContext(DbContextOptions<HarborContext> options) : base(options) { }

        public DbSet<Ship> Ships { get; set; }
    }
}