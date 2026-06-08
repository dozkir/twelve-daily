using TwelveDaily.Domain.Entities;

namespace TwelveDaily.Domain.Interfaces;

public interface IHabitScheduleRepository
{
    Task<List<HabitSchedule>> GetByHabitIdAsync(Guid habitId, CancellationToken ct = default);
    Task<List<HabitSchedule>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<List<HabitSchedule>> GetActiveByUserAndDayAsync(Guid userId, DayOfWeek dayOfWeek, CancellationToken ct = default);
    Task AddAsync(HabitSchedule schedule, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<HabitSchedule> schedules, CancellationToken ct = default);
    Task UpdateAsync(HabitSchedule schedule, CancellationToken ct = default);
    Task DeleteByHabitIdAsync(Guid habitId, CancellationToken ct = default);
}

