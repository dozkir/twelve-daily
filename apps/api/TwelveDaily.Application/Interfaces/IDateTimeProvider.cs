namespace TwelveDaily.Application.Interfaces;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateOnly TodayUtc { get; }
}

