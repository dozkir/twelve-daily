using MediatR;
using TwelveDaily.Application.Interfaces;
using TwelveDaily.Application.Users.Commands;
using TwelveDaily.Application.Users.Queries;
using TwelveDaily.Domain.Entities;
using TwelveDaily.Domain.Exceptions;
using TwelveDaily.Domain.Interfaces;

namespace TwelveDaily.Application.Users.Handlers;

public class GetUserProfileHandler : IRequestHandler<GetUserProfileQuery, UserProfileResult>
{
    private readonly IUserRepository _userRepository;

    public GetUserProfileHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserProfileResult> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            throw new DomainException("User not found.");

        return new UserProfileResult(user.Id, user.Email, user.Timezone, user.CreatedAt);
    }
}

public class UpdateTimezoneHandler : IRequestHandler<UpdateTimezoneCommand>
{
    private readonly IUserRepository _userRepository;

    public UpdateTimezoneHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task Handle(UpdateTimezoneCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            throw new DomainException("User not found.");

        user.UpdateTimezone(request.Timezone);
        await _userRepository.UpdateAsync(user, cancellationToken);
    }
}

public class UpdatePasswordHandler : IRequestHandler<UpdatePasswordCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UpdatePasswordHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task Handle(UpdatePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            throw new DomainException("User not found.");

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new DomainException("Current password is incorrect.");

        var newHash = _passwordHasher.Hash(request.NewPassword);
        user.UpdatePassword(newHash);
        await _userRepository.UpdateAsync(user, cancellationToken);
    }
}

public class RegisterPushTokenHandler : IRequestHandler<RegisterPushTokenCommand>
{
    private readonly IPushTokenRepository _pushTokenRepository;

    public RegisterPushTokenHandler(IPushTokenRepository pushTokenRepository)
    {
        _pushTokenRepository = pushTokenRepository;
    }

    public async Task Handle(RegisterPushTokenCommand request, CancellationToken cancellationToken)
    {
        var existing = await _pushTokenRepository.GetByTokenAsync(request.Token, cancellationToken);
        if (existing != null)
        {
            existing.Update(request.UserId, request.DeviceLabel);
            await _pushTokenRepository.UpdateAsync(existing, cancellationToken);
        }
        else
        {
            var pushToken = new PushToken(request.UserId, request.Token, request.DeviceLabel);
            await _pushTokenRepository.AddAsync(pushToken, cancellationToken);
        }
    }
}

public class SendRemoteTestNotificationHandler : IRequestHandler<SendRemoteTestNotificationCommand>
{
    private readonly IPushNotificationOrchestrator _pushNotificationOrchestrator;

    public SendRemoteTestNotificationHandler(IPushNotificationOrchestrator pushNotificationOrchestrator)
    {
        _pushNotificationOrchestrator = pushNotificationOrchestrator;
    }

    public async Task Handle(SendRemoteTestNotificationCommand request, CancellationToken cancellationToken)
    {
        await _pushNotificationOrchestrator.RecomputeUserNotificationsAsync(request.UserId, cancellationToken);
    }
}

