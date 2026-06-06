namespace TwelveDaily.Application.Interfaces;

public interface IGoogleCalendarService
{
    Task<string?> CreateEventAsync(
        Guid userId,
        string title,
        string? description,
        DateTime startUtc,
        DateTime endUtc,
        string userTimezone,
        CancellationToken ct = default);
}

