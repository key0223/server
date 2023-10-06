using addkeyserver.DTO;
using Microsoft.EntityFrameworkCore;

namespace addkeyserver.Database
{
    public class AppDbContext : DbContext
    {
        public DbSet<UserDb> Users { get; set; }
        public DbSet<PlayerDb> Players { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            Users = Set<UserDb>();
            Players = Set<PlayerDb>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserDb>().HasIndex(a => a.UserDbId).IsUnique();
            modelBuilder.Entity<PlayerDb>().HasIndex(a => a.PlayerDbId).IsUnique();
        }

    }
}
