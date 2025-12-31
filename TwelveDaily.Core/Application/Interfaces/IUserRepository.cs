using TwelveDaily.Core.Domains.Users;

namespace TwelveDaily.Core.Application.Interfaces;

public interface IUserRepository
{
    Task AddAsync(User user);
    Task<User?> GetUserByIdAsync(int id);
}