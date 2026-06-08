using TwelveDaily.Domain.Exceptions;

namespace TwelveDaily.Domain.Entities;

public class PushToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public string? DeviceLabel { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private PushToken() { } // EF Core

    public PushToken(Guid userId, string token, string? deviceLabel)
    {
        if (userId == Guid.Empty)
            throw new DomainException("UserId is required.");
        if (string.IsNullOrEmpty(token))
            throw new DomainException("Token is required.");

        Id = Guid.NewGuid();
        UserId = userId;
        Token = token;
        DeviceLabel = deviceLabel;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(Guid userId, string? deviceLabel)
    {
        if (userId == Guid.Empty)
            throw new DomainException("UserId is required.");

        UserId = userId;
        DeviceLabel = deviceLabel;
        UpdatedAt = DateTime.UtcNow;
    }
}
