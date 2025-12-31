using TwelveDaily.Core.Domains.Habits;

namespace TwelveDaily.Core.Application.Interfaces;

public interface IHabitRepository
{
    Task AddAsync(Habit habit);
    Task<Habit?> GetHabitByIdAsync(int id);
    Task<IReadOnlyList<Habit?>> GetAllUserHabitsAsync(int id);
}