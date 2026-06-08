using TwelveDaily.Domain.Entities;

namespace TwelveDaily.Domain.Interfaces;

public interface IGoogleConnectionRepository
{
    Task<GoogleConnection?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(GoogleConnection connection, CancellationToken ct = default);
    Task UpdateAsync(GoogleConnection connection, CancellationToken ct = default);
}

