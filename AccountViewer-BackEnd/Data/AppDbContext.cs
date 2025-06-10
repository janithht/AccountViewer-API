using AccountsViewer.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccountsViewer.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<ApplicationUser> Users { get; set; } = null!;
        public DbSet<Account> Accounts { get; set; }
        public DbSet<MonthlyBalance> MonthlyBalances { get; set; }
        public DbSet<UploadAudit> UploadAudits { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Account>().HasData(
                new Account { AccountId = 1, Name = "R&D" },
                new Account { AccountId = 2, Name = "Canteen" },
                new Account { AccountId = 3, Name = "CEO's car" },
                new Account { AccountId = 4, Name = "Marketing" },
                new Account { AccountId = 5, Name = "Parking fines" }
            );

            modelBuilder.Entity<MonthlyBalance>()
                .HasIndex(mb => new { mb.AccountId, mb.Year, mb.Month })
                .IsUnique();

            modelBuilder.Entity<ApplicationUser>().HasData(
                new ApplicationUser
                {
                    Id = 1,
                    Username = "admin@99x",
                    //Admin123!
                    PasswordHash = "$2b$12$wMH.912CBrm44SnwSueqyONVJ57p9IHNZZ1bT9XXbL26UebXt5DwW",
                    Role = "Admin"
                },
                new ApplicationUser
                {
                    Id = 2,
                    Username = "user@99x",
                    //User123!
                    PasswordHash = "$2b$12$VQQ6E2E5e.JVvkC/CEE1l.pJSVRb1lqiAMYhlydaYrsrJGbsk5IIi",
                    Role = "User"
                }
            );
        }
    }
}
