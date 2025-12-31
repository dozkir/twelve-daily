using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using TwelveDaily.Core.Api.Dto;
using TwelveDaily.Core.Infrastructure.Data;
using TwelveDaily.Core.Domains.Users;

namespace TwelveDaily.Core.Api.Controllers;

[ApiController]
[Route("api/v1/users")] // [Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly AppDbContext _context;

    public UserController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet] // GET /api/v1/user
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _context.Users.ToListAsync();
        return Ok(users);
    }

    [HttpGet("{id}")] // GET /api/v1/user/{id}
    public async Task<IActionResult> GetUserByID(int id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound();

        return Ok(user);
    }

    [HttpGet("get-by-email")] // GET /api/v1/user/get-by-email?email=...
    public async Task<IActionResult> GetUserByEmail([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest("Email is required");
        }
            
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            return NotFound("User not found");
        }

        return Ok(user);
    }

    [HttpPost] // POST /api/usuario
    public async Task<IActionResult> CreateUser([FromBody] UserCreateDto userCreateDto)
    {
        // newUserFromDto(userCreateDto)
        User newUser = new User
        (
            userCreateDto.Name,
            userCreateDto.Email,
            userCreateDto.PlainTextPassword
        );

        _context.Users.Add(newUser); // Marcando para inserção
        await _context.SaveChangesAsync(); // Salvando as alterações no banco

        return CreatedAtAction(
            nameof(GetUserByID),
            new { id = newUser.Id },
            newUser
        );
    }

    [HttpPut("{id}")] // PUT /api/usuario/{id}
    public async Task<IActionResult> UpdateUser(int id, UserUpdateDto updatedUser)
    {
        try
        {
            var currentUser = await _context.Users.FindAsync(id);

            if (currentUser == null)
            {
                return NotFound("User not found");
            }

            currentUser.Name = updatedUser.Name;
            currentUser.Email = updatedUser.Email;

            await _context.SaveChangesAsync();

            return Ok(currentUser);
        }
        catch (System.Exception exception)
        {
            return BadRequest("Request error: " + exception.Message);
        }
    }

    [HttpDelete("{id}")] // DELETE /api/usuario/{id}
    public async Task<ActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound("User not found");
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent(); // 204
    }
}
