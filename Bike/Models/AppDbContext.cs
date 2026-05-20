using Microsoft.EntityFrameworkCore;

namespace Bike.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<FuelLog> FuelLogs { get; set; }
    }
}