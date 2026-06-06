using Microsoft.EntityFrameworkCore;
using TwelveDaily.Domain.Entities;
using TwelveDaily.Domain.Interfaces;
using TwelveDaily.Infrastructure.Data;

namespace TwelveDaily.Infrastructure.Repositories;

public class HabitRepository : IHabitRepository
{
    private readonly AppDbContext _db;
    public HabitRepository(AppDbContext db) => _db = db;

    public async Task<Habit?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Habits.FindAsync([id], ct);

    public async Task<List<Habit>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _db.Habits.Where(h => h.UserId == userId).ToListAsync(ct);

    public async Task AddAsync(Habit habit, CancellationToken ct = default)
    {
        _db.Habits.Add(habit);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Habit habit, CancellationToken ct = default)
    {
        _db.Habits.Update(habit);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Habit habit, CancellationToken ct = default)
    {
        _db.Habits.Remove(habit);
        await _db.SaveChangesAsync(ct);
    }
}

