using Microsoft.EntityFrameworkCore;
using TwelveDaily.Api.Models;

namespace TwelveDaily.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("usuarios"); // CREATE TABLE usuarios
            e.HasKey(u => u.Id); // PRIMARY KEY (Id)
            e.HasIndex(u => u.Email).IsUnique(); // UNIQUE INDEX idx_email
            e.Property(u => u.Email).IsRequired().HasMaxLength(160); // VARCHAR(160) NOT NULL
            e.Property(u => u.Nome).IsRequired().HasMaxLength(160); // VARCHAR(160) NOT NULL
            e.Property(u => u.SenhaHash).IsRequired(); // NOT NULL
            e.Property(u => u.DataCriacao).HasDefaultValueSql("NOW()"); // DEFAULT NOW()
        });
    }
}

