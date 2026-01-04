using Microsoft.EntityFrameworkCore;

namespace DemoCiCdAzureApi.Data
{
    public class UserDbContext(DbContextOptions<UserDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
        public DbSet<Entities.User> Users { get; set; }
    }
}