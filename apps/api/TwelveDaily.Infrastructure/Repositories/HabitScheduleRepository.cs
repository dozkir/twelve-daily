using Microsoft.EntityFrameworkCore;
using TwelveDaily.Domain.Entities;
using TwelveDaily.Domain.Interfaces;
using TwelveDaily.Infrastructure.Data;

namespace TwelveDaily.Infrastructure.Repositories;

public class HabitScheduleRepository : IHabitScheduleRepository
{
    private readonly AppDbContext _db;
    public HabitScheduleRepository(AppDbContext db) => _db = db;

    public async Task<List<HabitSchedule>> GetByHabitIdAsync(Guid habitId, CancellationToken ct = default)
        => await _db.HabitSchedules.Where(s => s.HabitId == habitId).ToListAsync(ct);

    public async Task<List<HabitSchedule>> GetByUserAsync(Guid userId, CancellationToken ct = default)
    {
        var userHabitIds = _db.Habits
            .Where(h => h.UserId == userId)
            .Select(h => h.Id);

        return await _db.HabitSchedules
            .Where(s => userHabitIds.Contains(s.HabitId))
            .ToListAsync(ct);
    }

    public async Task<List<HabitSchedule>> GetActiveByUserAndDayAsync(Guid userId, DayOfWeek dayOfWeek, CancellationToken ct = default)
    {
        var userHabitIds = _db.Habits
            .Where(h => h.UserId == userId && h.IsActive)
            .Select(h => h.Id);

        return await _db.HabitSchedules
            .Where(s => userHabitIds.Contains(s.HabitId) && s.DayOfWeek == dayOfWeek && s.IsActive)
            .ToListAsync(ct);
    }

    public async Task AddAsync(HabitSchedule schedule, CancellationToken ct = default)
    {
        _db.HabitSchedules.Add(schedule);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddRangeAsync(IEnumerable<HabitSchedule> schedules, CancellationToken ct = default)
    {
        _db.HabitSchedules.AddRange(schedules);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(HabitSchedule schedule, CancellationToken ct = default)
    {
        _db.HabitSchedules.Update(schedule);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteByHabitIdAsync(Guid habitId, CancellationToken ct = default)
    {
        var schedules = await _db.HabitSchedules.Where(s => s.HabitId == habitId).ToListAsync(ct);
        _db.HabitSchedules.RemoveRange(schedules);
        await _db.SaveChangesAsync(ct);
    }
}

