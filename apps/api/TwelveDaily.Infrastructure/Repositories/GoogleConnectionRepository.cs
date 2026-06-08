using Microsoft.EntityFrameworkCore;
using TwelveDaily.Domain.Entities;
using TwelveDaily.Domain.Interfaces;
using TwelveDaily.Infrastructure.Data;

namespace TwelveDaily.Infrastructure.Repositories;

public class GoogleConnectionRepository : IGoogleConnectionRepository
{
    private readonly AppDbContext _db;
    public GoogleConnectionRepository(AppDbContext db) => _db = db;

    public async Task<GoogleConnection?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _db.GoogleConnections.FirstOrDefaultAsync(g => g.UserId == userId, ct);

    public async Task AddAsync(GoogleConnection connection, CancellationToken ct = default)
    {
        _db.GoogleConnections.Add(connection);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(GoogleConnection connection, CancellationToken ct = default)
    {
        _db.GoogleConnections.Update(connection);
        await _db.SaveChangesAsync(ct);
    }
}

