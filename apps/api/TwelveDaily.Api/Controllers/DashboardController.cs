using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TwelveDaily.Application.Habits.Queries;

namespace TwelveDaily.Api.Controllers;

[ApiController]
[Authorize]
[Route("dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;
    public DashboardController(IMediator mediator) => _mediator = mediator;

    [HttpGet("weekly")]
    public async Task<IActionResult> GetWeekly([FromQuery] DateOnly weekStart)
    {
        var userId = GetUserId();
        var result = await _mediator.Send(new GetWeeklyDashboardQuery(userId, weekStart));
        return Ok(result);
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}

