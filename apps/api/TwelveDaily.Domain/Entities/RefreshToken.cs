using TwelveDaily.Domain.Exceptions;

namespace TwelveDaily.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    private RefreshToken() { } // EF Core

    public RefreshToken(Guid userId, string token, DateTime expiresAt)
    {
        if (userId == Guid.Empty)
            throw new DomainException("UserId is required.");
        if (string.IsNullOrEmpty(token))
            throw new DomainException("Token is required.");

        Id = Guid.NewGuid();
        UserId = userId;
        Token = token;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
    }

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAt;

    public bool IsRevoked => RevokedAt != null;

    public bool IsActive(DateTime utcNow) => !IsExpired(utcNow) && !IsRevoked;

    public void Revoke(DateTime utcNow)
    {
        if (IsRevoked)
            throw new DomainException("Token is already revoked.");
        RevokedAt = utcNow;
    }
}
