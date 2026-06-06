using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TwelveDaily.Application.Habits.Commands;
using TwelveDaily.Application.Habits.Queries;

namespace TwelveDaily.Api.Controllers;

[ApiController]
[Authorize]
[Route("habits")]
public class HabitsController : ControllerBase
{
    private readonly IMediator _mediator;
    public HabitsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateHabitRequest request)
    {
        var userId = GetUserId();
        var schedules = request.Schedules.Select(s =>
            new CreateHabitScheduleDto(s.DayOfWeek, s.StartTime, s.EndTime, s.IsActive)).ToList();

        var command = new CreateHabitCommand(
            userId, request.Name, request.Emoji, request.Description,
            request.SyncGoogleCalendar, schedules);

        var habitId = await _mediator.Send(command);
        return StatusCode(201, habitId);
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var userId = GetUserId();
        var result = await _mediator.Send(new GetHabitsListQuery(userId));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDetail(Guid id)
    {
        var userId = GetUserId();
        var result = await _mediator.Send(new GetHabitDetailQuery(id, userId));
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateHabitRequest request)
    {
        var userId = GetUserId();
        await _mediator.Send(new UpdateHabitCommand(
            id, userId, request.Name, request.Emoji, request.Description, request.SyncGoogleCalendar));
        
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        await _mediator.Send(new DeleteHabitCommand(id, userId));
        return NoContent();
    }

    [HttpPatch("{id:guid}/toggle")]
    public async Task<IActionResult> Toggle(Guid id)
    {
        var userId = GetUserId();
        await _mediator.Send(new ToggleHabitCommand(id, userId));
        return NoContent();
    }

    [HttpPut("{id:guid}/schedules")]
    public async Task<IActionResult> UpdateSchedules(Guid id, [FromBody] UpdateSchedulesRequest request)
    {
        var userId = GetUserId();
        var schedules = request.Schedules.Select(s =>
            new CreateHabitScheduleDto(s.DayOfWeek, s.StartTime, s.EndTime, s.IsActive)).ToList();
        await _mediator.Send(new UpdateHabitSchedulesCommand(id, userId, schedules));
        return NoContent();
    }

    [HttpPatch("{id:guid}/schedules/{dayOfWeek}/toggle")]
    public async Task<IActionResult> ToggleSchedule(Guid id, string dayOfWeek)
    {
        var userId = GetUserId();
        var dow = Enum.Parse<DayOfWeek>(dayOfWeek, ignoreCase: true);
        await _mediator.Send(new ToggleHabitScheduleCommand(id, userId, dow));
        return NoContent();
    }

    [HttpGet("daily")]
    public async Task<IActionResult> GetDaily([FromQuery] DateOnly date)
    {
        var userId = GetUserId();
        var userTimezone = "UTC"; // Will be resolved by handler
        var result = await _mediator.Send(new GetDailyHabitsQuery(userId, date, userTimezone));
        return Ok(result);
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}

public record CreateHabitRequest(
    string Name,
    string Emoji,
    string? Description,
    bool SyncGoogleCalendar,
    List<CreateScheduleRequest> Schedules
);

public record CreateScheduleRequest(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsActive
);

public record UpdateHabitRequest(
    string Name,
    string Emoji,
    string? Description,
    bool SyncGoogleCalendar
);

public record UpdateSchedulesRequest(
    List<CreateScheduleRequest> Schedules
);

