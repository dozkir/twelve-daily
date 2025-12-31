using TwelveDaily.Core.Application.Interfaces;
using TwelveDaily.Core.Infrastructure.Data;
using TwelveDaily.Core.Domains.Users;

namespace TwelveDaily.Core.Infrastructure.Repositories;

public class UserRepository(AppDbContext context) : IUserRepository
{
    public async Task AddAsync(User user)
    {
        context.Users.Add(user);
        await context.SaveChangesAsync();
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await context.Users.FindAsync(userId);
    }
}