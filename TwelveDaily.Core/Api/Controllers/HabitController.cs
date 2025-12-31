using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// Dto
using TwelveDaily.Core.Api.Dto;

// Infrastructure
using TwelveDaily.Core.Infrastructure.Data;

// Application - Habits
using TwelveDaily.Core.Application.Habits.CreateHabit;
//using TwelveDaily.Core.Application.Habits.DeleteHabit;
using TwelveDaily.Core.Application.Habits.GetAllUserHabits;
using TwelveDaily.Core.Application.Habits.GetHabitById;
using TwelveDaily.Core.Application.Habits.UpdateHabit;

namespace TwelveDaily.Core.Api.Controllers;

[ApiController]
[Route("api/v1/habits")]
public class HabitController(
    AppDbContext context, 
    CreateHabitHandler createHabitHandler,
    GetAllUserHabitsHandler getAllUserHabitsHandler,
    GetHabitByIdHandler getHabitByIdHandler
) : ControllerBase
{
    // GET - e.g.: "api/v1/habits?userid=1"
    [HttpGet]
    public async Task<IActionResult> GetHabitsByUserId([FromQuery] int userId)
    {
        var query = new GetAllUserHabitsQuery(userId);
        var habitsResult = await getAllUserHabitsHandler.ExecuteAsync(query);

        if (!habitsResult.Success)
        {
            return BadRequest(habitsResult.Errors);
        }
        
        return Ok(habitsResult.Value);
    }

    // GET - e.g.: "api/v1/habits/1"
    [HttpGet("{habitId:int}")]
    public async Task<IActionResult> GetHabitById(int habitId)
    {
        var query = new GetHabitByIdQuery(habitId);
        var habitResult = await getHabitByIdHandler.ExecuteAsync(query);

        if (!habitResult.Success)
        {
            return BadRequest(habitResult.Errors);
        }
        
        if (habitResult.Value == null)
        {
            return NotFound("Habit '"+habitId+"' not found");
        }
        
        return Ok(habitResult.Value);
    }
    
    // POST - e.g.: "api/v1/habits"
    [HttpPost]
    public async Task<IActionResult> NewHabit([FromBody] CreateHabitDto dto)
    {
        var input = new CreateHabitCommand(
            1, // Temporary test user. Remember to add this to dto.
            dto.Name,
            dto.Description,
            dto.Icon,
            dto.Monday,
            dto.Tuesday,
            dto.Wednesday,
            dto.Thursday,
            dto.Friday,
            dto.Saturday,
            dto.Sunday
        );
    
        var createHabitResult = await createHabitHandler.ExecuteAsync(input);

        if (!createHabitResult.Success)
        {
            return BadRequest(createHabitResult.Errors);
        }

        return CreatedAtAction(
            nameof(GetHabitById),
            new { id = createHabitResult.Value},
            null
        );
    }

    // PUT - e.g.: "api/v1/habits/1"
    // [HttpPut("{habitId:int}")]
    // public async Task<IActionResult> UpdateHabitHandler(int habitId, [FromBody] UpdateHabitDto dto)
    // {
    //     
    // }
}