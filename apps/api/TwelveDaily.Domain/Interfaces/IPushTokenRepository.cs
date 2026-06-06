using TwelveDaily.Domain.Entities;

namespace TwelveDaily.Domain.Interfaces;

public interface IPushTokenRepository
{
    Task<List<PushToken>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<PushToken?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task AddAsync(PushToken pushToken, CancellationToken ct = default);
    Task UpdateAsync(PushToken pushToken, CancellationToken ct = default);
}

