using TwelveDaily.Domain.Exceptions;

namespace TwelveDaily.Domain.Entities;

public class Habit
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Emoji { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public bool SyncGoogleCalendar { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Habit() { } // EF Core

    public Habit(Guid userId, string name, string emoji, string? description, bool syncGoogleCalendar)
    {
        if (userId == Guid.Empty)
            throw new DomainException("UserId is required.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name is required.");
        if (string.IsNullOrWhiteSpace(emoji))
            throw new DomainException("Emoji is required.");

        Id = Guid.NewGuid();
        UserId = userId;
        Name = name;
        Emoji = emoji;
        Description = description;
        SyncGoogleCalendar = syncGoogleCalendar;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string emoji, string? description, bool syncGoogleCalendar)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name is required.");
        if (string.IsNullOrWhiteSpace(emoji))
            throw new DomainException("Emoji is required.");

        Name = name;
        Emoji = emoji;
        Description = description;
        SyncGoogleCalendar = syncGoogleCalendar;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ToggleActive()
    {
        IsActive = !IsActive;
        UpdatedAt = DateTime.UtcNow;
    }
}
