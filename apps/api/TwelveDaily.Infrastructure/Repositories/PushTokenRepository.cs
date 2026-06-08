using Microsoft.EntityFrameworkCore;
using TwelveDaily.Domain.Entities;
using TwelveDaily.Domain.Interfaces;
using TwelveDaily.Infrastructure.Data;

namespace TwelveDaily.Infrastructure.Repositories;

public class PushTokenRepository : IPushTokenRepository
{
    private readonly AppDbContext _db;
    public PushTokenRepository(AppDbContext db) => _db = db;

    public async Task<List<PushToken>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _db.PushTokens.Where(p => p.UserId == userId).ToListAsync(ct);

    public async Task<PushToken?> GetByTokenAsync(string token, CancellationToken ct = default)
        => await _db.PushTokens.FirstOrDefaultAsync(p => p.Token == token, ct);

    public async Task AddAsync(PushToken pushToken, CancellationToken ct = default)
    {
        _db.PushTokens.Add(pushToken);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(PushToken pushToken, CancellationToken ct = default)
    {
        _db.PushTokens.Update(pushToken);
        await _db.SaveChangesAsync(ct);
    }
}

