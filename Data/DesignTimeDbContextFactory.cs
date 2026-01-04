using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DemoCiCdAzureApi.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<UserDbContext>
    {
        public UserDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();
            // Use a file-based Sqlite DB for design-time migrations
            optionsBuilder.UseSqlite("Data Source=design_time.db");

            return new UserDbContext(optionsBuilder.Options);
        }
    }
}
