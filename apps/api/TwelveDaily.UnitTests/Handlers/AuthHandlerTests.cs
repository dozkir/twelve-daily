using FluentAssertions;
using NSubstitute;
using TwelveDaily.Application.Auth.Commands;
using TwelveDaily.Application.Auth.Handlers;
using TwelveDaily.Application.Interfaces;
using TwelveDaily.Domain.Entities;
using TwelveDaily.Domain.Exceptions;
using TwelveDaily.Domain.Interfaces;

namespace TwelveDaily.UnitTests.Handlers;

public class RegisterHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IDateTimeProvider _dateTime = Substitute.For<IDateTimeProvider>();
    private readonly RegisterHandler _handler;

    public RegisterHandlerTests()
    {
        _dateTime.UtcNow.Returns(DateTime.UtcNow);
        _handler = new RegisterHandler(_userRepository, _refreshTokenRepository, _passwordHasher, _tokenService, _dateTime);
    }

    [Fact]
    public async Task Handle_WithNewEmail_ShouldCreateUserAndReturnTokens()
    {
        var command = new RegisterCommand("new@example.com", "Password123!", "America/Sao_Paulo");
        _userRepository.GetByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns((User?)null);
        _passwordHasher.Hash(command.Password).Returns("hashed");
        _tokenService.GenerateAccessToken(Arg.Any<Guid>(), command.Email).Returns("access_token");
        _tokenService.GenerateRefreshToken().Returns("refresh_token");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_token");
        await _userRepository.Received(1).AddAsync(Arg.Is<User>(u => u.Email == "new@example.com"), Arg.Any<CancellationToken>());
        await _refreshTokenRepository.Received(1).AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithExistingEmail_ShouldThrow()
    {
        var command = new RegisterCommand("existing@example.com", "Password123!", "America/Sao_Paulo");
        var existingUser = new User("existing@example.com", "hash", "America/Sao_Paulo");
        _userRepository.GetByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(existingUser);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}

public class LoginHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IDateTimeProvider _dateTime = Substitute.For<IDateTimeProvider>();
    private readonly LoginHandler _handler;

    public LoginHandlerTests()
    {
        _dateTime.UtcNow.Returns(DateTime.UtcNow);
        _handler = new LoginHandler(_userRepository, _refreshTokenRepository, _passwordHasher, _tokenService, _dateTime);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnTokens()
    {
        var user = new User("test@example.com", "hashed", "America/Sao_Paulo");
        _userRepository.GetByEmailAsync("test@example.com", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("Password123!", "hashed").Returns(true);
        _tokenService.GenerateAccessToken(user.Id, user.Email).Returns("access_token");
        _tokenService.GenerateRefreshToken().Returns("refresh_token");

        var result = await _handler.Handle(new LoginCommand("test@example.com", "Password123!"), CancellationToken.None);

        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_token");
    }

    [Fact]
    public async Task Handle_WithNonExistentEmail_ShouldThrow()
    {
        _userRepository.GetByEmailAsync("noone@example.com", Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = () => _handler.Handle(new LoginCommand("noone@example.com", "pass"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ShouldThrow()
    {
        var user = new User("test@example.com", "hashed", "America/Sao_Paulo");
        _userRepository.GetByEmailAsync("test@example.com", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("wrong_password", "hashed").Returns(false);

        var act = () => _handler.Handle(new LoginCommand("test@example.com", "wrong_password"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}

public class RefreshTokenHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IDateTimeProvider _dateTime = Substitute.For<IDateTimeProvider>();
    private readonly RefreshTokenHandler _handler;

    public RefreshTokenHandlerTests()
    {
        _dateTime.UtcNow.Returns(DateTime.UtcNow);
        _handler = new RefreshTokenHandler(_refreshTokenRepository, _userRepository, _tokenService, _dateTime);
    }

    [Fact]
    public async Task Handle_WithValidToken_ShouldRotateAndReturnNewTokens()
    {
        var userId = Guid.NewGuid();
        var oldToken = new RefreshToken(userId, "old_refresh", DateTime.UtcNow.AddDays(7));
        var user = new User("test@example.com", "hash", "America/Sao_Paulo");

        _refreshTokenRepository.GetByTokenAsync("old_refresh", Arg.Any<CancellationToken>()).Returns(oldToken);
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _tokenService.GenerateAccessToken(userId, user.Email).Returns("new_access");
        _tokenService.GenerateRefreshToken().Returns("new_refresh");

        var result = await _handler.Handle(new RefreshTokenCommand("old_refresh"), CancellationToken.None);

        result.AccessToken.Should().Be("new_access");
        result.RefreshToken.Should().Be("new_refresh");
    }

    [Fact]
    public async Task Handle_WithExpiredToken_ShouldThrow()
    {
        var expiredToken = new RefreshToken(Guid.NewGuid(), "expired", DateTime.UtcNow.AddDays(-1));
        _refreshTokenRepository.GetByTokenAsync("expired", Arg.Any<CancellationToken>()).Returns(expiredToken);

        var act = () => _handler.Handle(new RefreshTokenCommand("expired"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_WithNonExistentToken_ShouldThrow()
    {
        _refreshTokenRepository.GetByTokenAsync("nonexistent", Arg.Any<CancellationToken>()).Returns((RefreshToken?)null);

        var act = () => _handler.Handle(new RefreshTokenCommand("nonexistent"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}

public class LogoutHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IDateTimeProvider _dateTime = Substitute.For<IDateTimeProvider>();
    private readonly LogoutHandler _handler;

    public LogoutHandlerTests()
    {
        _dateTime.UtcNow.Returns(DateTime.UtcNow);
        _handler = new LogoutHandler(_refreshTokenRepository, _dateTime);
    }

    [Fact]
    public async Task Handle_WithValidToken_ShouldRevokeToken()
    {
        var userId = Guid.NewGuid();
        var token = new RefreshToken(userId, "token_to_revoke", DateTime.UtcNow.AddDays(7));
        _refreshTokenRepository.GetByTokenAsync("token_to_revoke", Arg.Any<CancellationToken>()).Returns(token);

        await _handler.Handle(new LogoutCommand(userId, "token_to_revoke"), CancellationToken.None);

        await _refreshTokenRepository.Received(1).UpdateAsync(Arg.Is<RefreshToken>(t => t.RevokedAt != null), Arg.Any<CancellationToken>());
    }
}

public class LogoutAllHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IDateTimeProvider _dateTime = Substitute.For<IDateTimeProvider>();
    private readonly LogoutAllHandler _handler;

    public LogoutAllHandlerTests()
    {
        _dateTime.UtcNow.Returns(DateTime.UtcNow);
        _handler = new LogoutAllHandler(_refreshTokenRepository, _dateTime);
    }

    [Fact]
    public async Task Handle_ShouldRevokeAllActiveTokens()
    {
        var userId = Guid.NewGuid();
        var tokens = new List<RefreshToken>
        {
            new(userId, "token1", DateTime.UtcNow.AddDays(7)),
            new(userId, "token2", DateTime.UtcNow.AddDays(7))
        };
        _refreshTokenRepository.GetActiveByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(tokens);

        await _handler.Handle(new LogoutAllCommand(userId), CancellationToken.None);

        await _refreshTokenRepository.Received(2).UpdateAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }
}

