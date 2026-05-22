using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bike.Data
{
    public class BikeDbContextFactory : IDesignTimeDbContextFactory<BikeDbContext>
    {
        public BikeDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<BikeDbContext>();

            optionsBuilder.UseNpgsql(
                "Host=localhost;Port=5433;Database=sandbox;Username=postgres;Password=postgres");

            return new BikeDbContext(optionsBuilder.Options);
        }
    }
}