using Microsoft.EntityFrameworkCore;
using TwelveDaily.Domain.Entities;
using TwelveDaily.Domain.Interfaces;
using TwelveDaily.Infrastructure.Data;

namespace TwelveDaily.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _db;
    public RefreshTokenRepository(AppDbContext db) => _db = db;

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default)
        => await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token, ct);

    public async Task<List<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(ct);

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default)
    {
        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(RefreshToken refreshToken, CancellationToken ct = default)
    {
        _db.RefreshTokens.Update(refreshToken);
        await _db.SaveChangesAsync(ct);
    }
}

