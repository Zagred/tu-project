using BankAPP.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace BankAPP.Shared.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<UserAccount> UserAccounts { get; set; }
        public DbSet<Movement> Movements { get; set; }
        public DbSet<Card> Cards { get; set; }
        public DbSet<Merchant> Merchants { get; set; }
        public DbSet<Location> Locations { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Egn)
                .IsUnique();
        }
    }
}