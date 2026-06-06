using Microsoft.EntityFrameworkCore;
using TwelveDaily.Domain.Entities;
using TwelveDaily.Domain.Interfaces;
using TwelveDaily.Infrastructure.Data;

namespace TwelveDaily.Infrastructure.Repositories;

public class HabitCheckRepository : IHabitCheckRepository
{
    private readonly AppDbContext _db;
    public HabitCheckRepository(AppDbContext db) => _db = db;

    public async Task<HabitCheck?> GetByHabitAndDateAsync(Guid habitId, DateOnly date, CancellationToken ct = default)
        => await _db.HabitChecks.FirstOrDefaultAsync(c => c.HabitId == habitId && c.Date == date, ct);

    public async Task<bool> ExistsAsync(Guid habitId, DateOnly date, CancellationToken ct = default)
        => await _db.HabitChecks.AnyAsync(c => c.HabitId == habitId && c.Date == date, ct);

    public async Task<List<HabitCheck>> GetByUserAndDateRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken ct = default)
        => await _db.HabitChecks
            .Where(c => c.UserId == userId && c.Date >= startDate && c.Date <= endDate)
            .ToListAsync(ct);

    public async Task<HashSet<Guid>> GetCheckedHabitIdsAsync(Guid userId, DateOnly date, CancellationToken ct = default)
    {
        var ids = await _db.HabitChecks
            .Where(c => c.UserId == userId && c.Date == date)
            .Select(c => c.HabitId)
            .ToListAsync(ct);
        return ids.ToHashSet();
    }

    public async Task AddAsync(HabitCheck check, CancellationToken ct = default)
    {
        _db.HabitChecks.Add(check);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(HabitCheck check, CancellationToken ct = default)
    {
        _db.HabitChecks.Remove(check);
        await _db.SaveChangesAsync(ct);
    }
}
