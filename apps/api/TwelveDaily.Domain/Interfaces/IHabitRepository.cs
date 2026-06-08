using TwelveDaily.Domain.Entities;

namespace TwelveDaily.Domain.Interfaces;

public interface IHabitRepository
{
    Task<Habit?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Habit>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(Habit habit, CancellationToken ct = default);
    Task UpdateAsync(Habit habit, CancellationToken ct = default);
    Task DeleteAsync(Habit habit, CancellationToken ct = default);
}

