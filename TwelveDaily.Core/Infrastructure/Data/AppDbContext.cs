using Microsoft.EntityFrameworkCore;
using TwelveDaily.Core.Domains.Habits;
using TwelveDaily.Core.Domains.Users;

namespace TwelveDaily.Core.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Habit> Habits => Set<Habit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users"); // CREATE TABLE users
            e.HasKey(u => u.Id); // PRIMARY KEY (Id)
            e.HasIndex(u => u.Email).IsUnique(); // UNIQUE INDEX idx_email
            e.Property(u => u.Email).IsRequired().HasMaxLength(160); // VARCHAR(160) NOT NULL
            e.Property(u => u.Name).IsRequired().HasMaxLength(160); // VARCHAR(160) NOT NULL
            e.Property(u => u.HashedPassword).IsRequired(); // NOT NULL
            e.Property(u => u.CreatedAt).HasDefaultValueSql("NOW()"); // DEFAULT NOW()
        });
        
        modelBuilder.Entity<Habit>(e =>
        {
            e.ToTable("habits");
            e.HasKey(h => h.Id);
            e.Property(h => h.Name).IsRequired().HasMaxLength(160);
            e.Property(h => h.Description).HasMaxLength(1024);
            e.Property(h => h.Enabled).HasDefaultValue(true);
            e.Property(h => h.Icon).HasMaxLength(120);
            e.Property(h => h.CreatedAt).HasDefaultValueSql("NOW()");
            e.Property(h => h.ModifiedAt).HasDefaultValueSql("NOW()");

            // Relationship: one User has many Habits
            e.HasOne<User>()
                .WithMany()
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Owned value object for week schedule
            e.OwnsOne(h => h.WeekSchedule, ws =>
            {
                ws.Property(p => p.Monday);
                ws.Property(p => p.Tuesday);
                ws.Property(p => p.Wednesday);
                ws.Property(p => p.Thursday);
                ws.Property(p => p.Friday);
                ws.Property(p => p.Saturday);
                ws.Property(p => p.Sunday);
                ws.Ignore(p => p.HasAnyDayDefined);
            });
        });
    }
}

