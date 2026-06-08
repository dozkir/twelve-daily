using MediatR;
using TwelveDaily.Application.Auth.Commands;
using TwelveDaily.Application.Interfaces;
using TwelveDaily.Domain.Entities;
using TwelveDaily.Domain.Exceptions;
using TwelveDaily.Domain.Interfaces;

namespace TwelveDaily.Application.Auth.Handlers;

public class RegisterHandler : IRequestHandler<RegisterCommand, AuthResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IDateTimeProvider _dateTime;

    public RegisterHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IDateTimeProvider dateTime)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _dateTime = dateTime;
    }

    public async Task<AuthResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existing = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing != null)
            throw new ConflictException("Email already exists.");

        var hash = _passwordHasher.Hash(request.Password);
        var user = new User(request.Email, hash, request.Timezone);
        await _userRepository.AddAsync(user, cancellationToken);

        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        var refreshTokenExpiry = _dateTime.UtcNow.AddDays(7);
        var refreshToken = new RefreshToken(user.Id, refreshTokenValue, refreshTokenExpiry);
        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        return new AuthResult(
            accessToken,
            _dateTime.UtcNow.AddMinutes(15),
            refreshTokenValue,
            refreshTokenExpiry);
    }
}

public class LoginHandler : IRequestHandler<LoginCommand, AuthResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IDateTimeProvider _dateTime;

    public LoginHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IDateTimeProvider dateTime)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _dateTime = dateTime;
    }

    public async Task<AuthResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
            throw new UnauthorizedException("Invalid credentials.");

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid credentials.");

        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        var refreshTokenExpiry = _dateTime.UtcNow.AddDays(7);
        var refreshToken = new RefreshToken(user.Id, refreshTokenValue, refreshTokenExpiry);
        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        return new AuthResult(
            accessToken,
            _dateTime.UtcNow.AddMinutes(15),
            refreshTokenValue,
            refreshTokenExpiry);
    }
}

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, AuthResult>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IDateTimeProvider _dateTime;

    public RefreshTokenHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        ITokenService tokenService,
        IDateTimeProvider dateTime)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _tokenService = tokenService;
        _dateTime = dateTime;
    }

    public async Task<AuthResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var token = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);
        if (token == null || !token.IsActive(_dateTime.UtcNow))
            throw new UnauthorizedException("Invalid or expired refresh token.");

        token.Revoke(_dateTime.UtcNow);
        await _refreshTokenRepository.UpdateAsync(token, cancellationToken);

        var user = await _userRepository.GetByIdAsync(token.UserId, cancellationToken);
        var accessToken = _tokenService.GenerateAccessToken(token.UserId, user!.Email);
        var newRefreshTokenValue = _tokenService.GenerateRefreshToken();
        var refreshTokenExpiry = _dateTime.UtcNow.AddDays(7);
        var newRefreshToken = new RefreshToken(token.UserId, newRefreshTokenValue, refreshTokenExpiry);
        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

        return new AuthResult(
            accessToken,
            _dateTime.UtcNow.AddMinutes(15),
            newRefreshTokenValue,
            refreshTokenExpiry);
    }
}

public class LogoutHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IDateTimeProvider _dateTime;

    public LogoutHandler(IRefreshTokenRepository refreshTokenRepository, IDateTimeProvider dateTime)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _dateTime = dateTime;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var token = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);
        if (token == null) return;

        token.Revoke(_dateTime.UtcNow);
        await _refreshTokenRepository.UpdateAsync(token, cancellationToken);
    }
}

public class LogoutAllHandler : IRequestHandler<LogoutAllCommand>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IDateTimeProvider _dateTime;

    public LogoutAllHandler(IRefreshTokenRepository refreshTokenRepository, IDateTimeProvider dateTime)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _dateTime = dateTime;
    }

    public async Task Handle(LogoutAllCommand request, CancellationToken cancellationToken)
    {
        var tokens = await _refreshTokenRepository.GetActiveByUserIdAsync(request.UserId, cancellationToken);
        foreach (var token in tokens)
        {
            token.Revoke(_dateTime.UtcNow);
            await _refreshTokenRepository.UpdateAsync(token, cancellationToken);
        }
    }
}
