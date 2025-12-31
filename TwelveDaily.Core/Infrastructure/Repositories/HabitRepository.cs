using Microsoft.EntityFrameworkCore;
using TwelveDaily.Core.Infrastructure.Data;
using TwelveDaily.Core.Domains.Habits;
using TwelveDaily.Core.Application.Interfaces;

namespace TwelveDaily.Core.Infrastructure.Repositories;

public class HabitRepository(AppDbContext context) : IHabitRepository
{
    public async Task AddAsync(Habit habit)
    {
        context.Habits.Add(habit);
        await context.SaveChangesAsync();
    }

    public async Task<Habit?> GetHabitByIdAsync(int id)
    {
        return await context.Habits.FindAsync(id);
    }

    public async Task<IReadOnlyList<Habit?>> GetAllUserHabitsAsync(int id)
    {
        return await context.Habits
            .Where(h => h.UserId == id)
            .ToListAsync();
    }
}