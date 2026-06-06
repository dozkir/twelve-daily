using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TwelveDaily.Application.Users.Commands;
using TwelveDaily.Application.Users.Queries;

namespace TwelveDaily.Api.Controllers;

[ApiController]
[Authorize]
[Route("users")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    public UsersController(IMediator mediator) => _mediator = mediator;

    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserId();
        var result = await _mediator.Send(new GetUserProfileQuery(userId));
        return Ok(result);
    }

    [HttpPut("me/timezone")]
    public async Task<IActionResult> UpdateTimezone([FromBody] UpdateTimezoneRequest request)
    {
        var userId = GetUserId();
        await _mediator.Send(new UpdateTimezoneCommand(userId, request.Timezone));
        return NoContent();
    }

    [HttpPut("me/password")]
    public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest request)
    {
        var userId = GetUserId();
        await _mediator.Send(new UpdatePasswordCommand(userId, request.CurrentPassword, request.NewPassword));
        return NoContent();
    }

    [HttpPost("push-token")]
    public async Task<IActionResult> RegisterPushToken([FromBody] RegisterPushTokenRequest request)
    {
        var userId = GetUserId();
        await _mediator.Send(new RegisterPushTokenCommand(userId, request.Token, request.DeviceLabel));
        return NoContent();
    }

    [HttpPost("push-test")]
    public async Task<IActionResult> SendRemotePushTest()
    {
        var userId = GetUserId();
        await _mediator.Send(new SendRemoteTestNotificationCommand(userId));
        return NoContent();
    }

    [HttpPost("push-sync")]
    public async Task<IActionResult> SyncActivePushNotification()
    {
        var userId = GetUserId();
        await _mediator.Send(new SendRemoteTestNotificationCommand(userId));
        return NoContent();
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}

public record UpdateTimezoneRequest(string Timezone);
public record UpdatePasswordRequest(string CurrentPassword, string NewPassword);
public record RegisterPushTokenRequest(string Token, string? DeviceLabel);

