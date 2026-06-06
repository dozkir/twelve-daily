using MediatR;

namespace TwelveDaily.Application.Users.Commands;

public record UpdateTimezoneCommand(Guid UserId, string Timezone) : IRequest;

public record UpdatePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : IRequest;

public record RegisterPushTokenCommand(Guid UserId, string Token, string? DeviceLabel) : IRequest;

public record SendRemoteTestNotificationCommand(Guid UserId) : IRequest;

