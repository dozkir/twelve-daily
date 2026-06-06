using TwelveDaily.Domain.Entities;

namespace TwelveDaily.Domain.Interfaces;

public interface IHabitCheckRepository
{
    Task<HabitCheck?> GetByHabitAndDateAsync(Guid habitId, DateOnly date, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid habitId, DateOnly date, CancellationToken ct = default);
    Task<List<HabitCheck>> GetByUserAndDateRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken ct = default);

    /// <summary>HabitIds que possuem check para o usuário na data informada.</summary>
    Task<HashSet<Guid>> GetCheckedHabitIdsAsync(Guid userId, DateOnly date, CancellationToken ct = default);

    Task AddAsync(HabitCheck check, CancellationToken ct = default);
    Task DeleteAsync(HabitCheck check, CancellationToken ct = default);
}
