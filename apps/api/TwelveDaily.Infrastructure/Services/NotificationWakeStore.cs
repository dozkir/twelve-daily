using Microsoft.EntityFrameworkCore;
using TwelveDaily.Infrastructure.Data;

namespace TwelveDaily.Infrastructure.Services;

/// <summary>
/// Persiste o id do job Hangfire de "próximo wake" por usuário (<see cref="NotificationWake"/>),
/// para que o orquestrador cancele o wake anterior antes de agendar o próximo.
/// </summary>
public class NotificationWakeStore
{
    private readonly AppDbContext _db;

    public NotificationWakeStore(AppDbContext db) => _db = db;

    public async Task<string?> GetJobIdAsync(Guid userId, CancellationToken ct = default)
    {
        var wake = await _db.NotificationWakes.FirstOrDefaultAsync(w => w.UserId == userId, ct);
        return wake?.JobId;
    }

    public async Task SetJobIdAsync(Guid userId, string jobId, CancellationToken ct = default)
    {
        var wake = await _db.NotificationWakes.FirstOrDefaultAsync(w => w.UserId == userId, ct);
        if (wake == null)
            _db.NotificationWakes.Add(new NotificationWake(userId, jobId));
        else
            wake.SetJob(jobId);

        await _db.SaveChangesAsync(ct);
    }

    public async Task ClearAsync(Guid userId, CancellationToken ct = default)
    {
        var wake = await _db.NotificationWakes.FirstOrDefaultAsync(w => w.UserId == userId, ct);
        if (wake == null)
            return;

        _db.NotificationWakes.Remove(wake);
        await _db.SaveChangesAsync(ct);
    }
}
