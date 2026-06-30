using Microsoft.EntityFrameworkCore;
using TwelveDaily.Domain.Entities;
using TwelveDaily.Infrastructure.Services;

namespace TwelveDaily.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Habit> Habits => Set<Habit>();
    public DbSet<HabitSchedule> HabitSchedules => Set<HabitSchedule>();
    public DbSet<HabitCheck> HabitChecks => Set<HabitCheck>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PushToken> PushTokens => Set<PushToken>();
    public DbSet<GoogleConnection> GoogleConnections => Set<GoogleConnection>();
    public DbSet<NotificationWake> NotificationWakes => Set<NotificationWake>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).IsRequired().HasMaxLength(256);
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.Timezone).IsRequired().HasMaxLength(64);
        });

        // Habit
        modelBuilder.Entity<Habit>(e =>
        {
            e.HasKey(h => h.Id);
            e.Property(h => h.Name).IsRequired().HasMaxLength(256);
            e.Property(h => h.Emoji).IsRequired().HasMaxLength(16);
            e.Property(h => h.Description).HasMaxLength(1024);
            e.HasIndex(h => h.UserId);
        });

        // HabitSchedule
        modelBuilder.Entity<HabitSchedule>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasOne<Habit>().WithMany().HasForeignKey(s => s.HabitId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(s => s.HabitId);
        });

        // HabitCheck
        modelBuilder.Entity<HabitCheck>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasOne<Habit>().WithMany().HasForeignKey(c => c.HabitId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(c => new { c.HabitId, c.Date }).IsUnique(); // 1 check por hábito por dia
            e.HasIndex(c => new { c.UserId, c.Date });
            e.Property(c => c.HabitName).IsRequired().HasMaxLength(256);
            e.Property(c => c.HabitEmoji).IsRequired().HasMaxLength(16);
        });

        // RefreshToken
        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.Token).IsUnique();
            e.Property(t => t.Token).IsRequired().HasMaxLength(512);
            e.HasIndex(t => t.UserId);
        });

        // PushToken
        modelBuilder.Entity<PushToken>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.Token).IsUnique();
            e.Property(p => p.Token).IsRequired().HasMaxLength(512);
            e.HasIndex(p => p.UserId);
        });

        // GoogleConnection
        modelBuilder.Entity<GoogleConnection>(e =>
        {
            e.HasKey(g => g.Id);
            e.HasIndex(g => g.UserId).IsUnique();
            e.Property(g => g.AccessToken).IsRequired();
            e.Property(g => g.RefreshToken).IsRequired();
        });

        // NotificationWake — 1 wake Hangfire pendente por usuário (chave = UserId).
        modelBuilder.Entity<NotificationWake>(e =>
        {
            e.HasKey(w => w.UserId);
            e.Property(w => w.JobId).IsRequired().HasMaxLength(128);
        });
    }
}

