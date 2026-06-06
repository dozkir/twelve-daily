using MediatR;

namespace TwelveDaily.Application.Auth.Commands;

public record RegisterCommand(string Email, string Password, string Timezone) : IRequest<AuthResult>;

public record LoginCommand(string Email, string Password) : IRequest<AuthResult>;

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResult>;

public record LogoutCommand(Guid UserId, string RefreshToken) : IRequest;

public record LogoutAllCommand(Guid UserId) : IRequest;

public record AuthResult(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);

