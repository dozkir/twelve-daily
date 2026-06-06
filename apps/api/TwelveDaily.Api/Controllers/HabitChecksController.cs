using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TwelveDaily.Application.Habits.Commands;

namespace TwelveDaily.Api.Controllers;

[ApiController]
[Authorize]
[Route("habits/{habitId:guid}/check")]
public class HabitChecksController : ControllerBase
{
    private readonly IMediator _mediator;
    public HabitChecksController(IMediator mediator) => _mediator = mediator;

    // Marca o hábito como concluído em uma data (upsert idempotente).
    [HttpPut]
    public async Task<IActionResult> Check(Guid habitId, [FromBody] CheckRequest request)
    {
        var userId = GetUserId();
        var result = await _mediator.Send(new CheckHabitCommand(userId, habitId, request.Date));
        return Ok(result);
    }

    // Desmarca o hábito em uma data (delete idempotente).
    [HttpDelete]
    public async Task<IActionResult> Uncheck(Guid habitId, [FromQuery] DateOnly date)
    {
        var userId = GetUserId();
        await _mediator.Send(new UncheckHabitCommand(userId, habitId, date));
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("from-notification")]
    public async Task<IActionResult> CheckFromNotification(Guid habitId, [FromBody] CheckFromNotificationRequest request)
    {
        var result = await _mediator.Send(new CheckHabitFromNotificationCommand(habitId, request.Date, request.ActionToken));
        return Ok(result);
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}

public record CheckRequest(DateOnly Date);
public record CheckFromNotificationRequest(DateOnly Date, string ActionToken);
