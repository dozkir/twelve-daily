using FluentAssertions;
using NSubstitute;
using TwelveDaily.Application.Interfaces;
using TwelveDaily.Application.Users.Commands;
using TwelveDaily.Application.Users.Handlers;
using TwelveDaily.Application.Users.Queries;
using TwelveDaily.Domain.Entities;
using TwelveDaily.Domain.Exceptions;
using TwelveDaily.Domain.Interfaces;

namespace TwelveDaily.UnitTests.Handlers;

public class GetUserProfileHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly GetUserProfileHandler _handler;

    public GetUserProfileHandlerTests()
    {
        _handler = new GetUserProfileHandler(_userRepository);
    }

    [Fact]
    public async Task Handle_WithExistingUser_ShouldReturnProfile()
    {
        var user = new User("test@example.com", "hash", "America/Sao_Paulo");
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(new GetUserProfileQuery(user.Id), CancellationToken.None);

        result.Email.Should().Be("test@example.com");
        result.Timezone.Should().Be("America/Sao_Paulo");
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldThrow()
    {
        _userRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = () => _handler.Handle(new GetUserProfileQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}

public class UpdateTimezoneHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly UpdateTimezoneHandler _handler;

    public UpdateTimezoneHandlerTests()
    {
        _handler = new UpdateTimezoneHandler(_userRepository);
    }

    [Fact]
    public async Task Handle_WithValidTimezone_ShouldUpdate()
    {
        var user = new User("test@example.com", "hash", "America/Sao_Paulo");
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        await _handler.Handle(new UpdateTimezoneCommand(user.Id, "Europe/Lisbon"), CancellationToken.None);

        await _userRepository.Received(1).UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldThrow()
    {
        _userRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = () => _handler.Handle(new UpdateTimezoneCommand(Guid.NewGuid(), "Europe/Lisbon"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}

public class UpdatePasswordHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly UpdatePasswordHandler _handler;

    public UpdatePasswordHandlerTests()
    {
        _handler = new UpdatePasswordHandler(_userRepository, _passwordHasher);
    }

    [Fact]
    public async Task Handle_WithCorrectCurrentPassword_ShouldUpdate()
    {
        var user = new User("test@example.com", "oldhash", "America/Sao_Paulo");
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("currentpass", "oldhash").Returns(true);
        _passwordHasher.Hash("newpass").Returns("newhash");

        await _handler.Handle(new UpdatePasswordCommand(user.Id, "currentpass", "newpass"), CancellationToken.None);

        await _userRepository.Received(1).UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithWrongCurrentPassword_ShouldThrow()
    {
        var user = new User("test@example.com", "oldhash", "America/Sao_Paulo");
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("wrongpass", "oldhash").Returns(false);

        var act = () => _handler.Handle(new UpdatePasswordCommand(user.Id, "wrongpass", "newpass"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldThrow()
    {
        _userRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = () => _handler.Handle(new UpdatePasswordCommand(Guid.NewGuid(), "pass", "newpass"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}

public class RegisterPushTokenHandlerTests
{
    private readonly IPushTokenRepository _pushTokenRepository = Substitute.For<IPushTokenRepository>();
    private readonly RegisterPushTokenHandler _handler;

    public RegisterPushTokenHandlerTests()
    {
        _handler = new RegisterPushTokenHandler(_pushTokenRepository);
    }

    [Fact]
    public async Task Handle_WithNewToken_ShouldAdd()
    {
        _pushTokenRepository.GetByTokenAsync("ExponentPushToken[xxx]", Arg.Any<CancellationToken>()).Returns((PushToken?)null);

        await _handler.Handle(new RegisterPushTokenCommand(Guid.NewGuid(), "ExponentPushToken[xxx]", "iPhone"), CancellationToken.None);

        await _pushTokenRepository.Received(1).AddAsync(Arg.Any<PushToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithExistingToken_ShouldUpdate()
    {
        var existing = new PushToken(Guid.NewGuid(), "ExponentPushToken[xxx]", "Old Label");
        _pushTokenRepository.GetByTokenAsync("ExponentPushToken[xxx]", Arg.Any<CancellationToken>()).Returns(existing);

        await _handler.Handle(new RegisterPushTokenCommand(Guid.NewGuid(), "ExponentPushToken[xxx]", "New Label"), CancellationToken.None);

        await _pushTokenRepository.Received(1).UpdateAsync(Arg.Any<PushToken>(), Arg.Any<CancellationToken>());
    }
}

public class SendRemoteTestNotificationHandlerTests
{
    private readonly IPushNotificationOrchestrator _pushNotificationOrchestrator = Substitute.For<IPushNotificationOrchestrator>();
    private readonly SendRemoteTestNotificationHandler _handler;

    public SendRemoteTestNotificationHandlerTests()
    {
        _handler = new SendRemoteTestNotificationHandler(_pushNotificationOrchestrator);
    }

    [Fact]
    public async Task Handle_ShouldTriggerRemoteRecompute()
    {
        var userId = Guid.NewGuid();

        await _handler.Handle(new SendRemoteTestNotificationCommand(userId), CancellationToken.None);

        await _pushNotificationOrchestrator.Received(1)
            .RecomputeUserNotificationsAsync(userId, Arg.Any<CancellationToken>());
    }
}

