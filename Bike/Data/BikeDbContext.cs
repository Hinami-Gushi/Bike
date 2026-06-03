using Bike.Models;
using Microsoft.EntityFrameworkCore;

namespace Bike.Data
{
    public class BikeDbContext : DbContext
    {
        public BikeDbContext(DbContextOptions<BikeDbContext> options)
            : base(options)
        {
        }

        public DbSet<FuelLog> FuelLogs { get; set; }
        public DbSet<User> Users { get; set; }
    }
}