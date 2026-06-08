using TwelveDaily.Domain.Exceptions;

namespace TwelveDaily.Domain.Entities;

public class GoogleConnection
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string AccessToken { get; private set; } = string.Empty;
    public string RefreshToken { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public string CalendarId { get; private set; } = "primary";
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private GoogleConnection() { } // EF Core

    public GoogleConnection(Guid userId, string accessToken, string refreshToken, DateTime expiresAt, string calendarId = "primary")
    {
        if (userId == Guid.Empty)
            throw new DomainException("UserId is required.");
        if (string.IsNullOrEmpty(accessToken))
            throw new DomainException("AccessToken is required.");
        if (string.IsNullOrEmpty(refreshToken))
            throw new DomainException("RefreshToken is required.");

        Id = Guid.NewGuid();
        UserId = userId;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresAt = expiresAt;
        CalendarId = calendarId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
