using TwelveDaily.Domain.Exceptions;

namespace TwelveDaily.Domain.Entities;

public class HabitSchedule
{
    public Guid Id { get; private set; }
    public Guid HabitId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private HabitSchedule() { } // EF Core

    public HabitSchedule(Guid habitId, DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime)
    {
        if (habitId == Guid.Empty)
            throw new DomainException("HabitId is required.");
        if (endTime <= startTime)
            throw new DomainException("EndTime must be after StartTime.");

        Id = Guid.NewGuid();
        HabitId = habitId;
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTime(TimeOnly startTime, TimeOnly endTime)
    {
        if (endTime <= startTime)
            throw new DomainException("EndTime must be after StartTime.");
        StartTime = startTime;
        EndTime = endTime;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ToggleActive()
    {
        IsActive = !IsActive;
        UpdatedAt = DateTime.UtcNow;
    }
}
