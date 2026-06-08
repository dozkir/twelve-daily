namespace TwelveDaily.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(Guid userId, string email);
    string GenerateRefreshToken();
}

