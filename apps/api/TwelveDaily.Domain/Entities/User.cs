using TwelveDaily.Domain.Exceptions;

namespace TwelveDaily.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Timezone { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private User() { } // EF Core

    public User(string email, string passwordHash, string timezone)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash is required.");
        ValidateTimezone(timezone);

        Id = Guid.NewGuid();
        Email = email;
        PasswordHash = passwordHash;
        Timezone = timezone;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTimezone(string timezone)
    {
        ValidateTimezone(timezone);
        Timezone = timezone;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePassword(string newPasswordHash)
    {
        if (string.IsNullOrEmpty(newPasswordHash))
            throw new DomainException("Password hash is required.");
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateTimezone(string? timezone)
    {
        if (string.IsNullOrWhiteSpace(timezone))
            throw new DomainException("Timezone is required.");
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timezone);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new DomainException($"Invalid timezone: {timezone}");
        }
    }
}
