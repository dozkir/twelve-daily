using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TwelveDaily.Api.Data;
using TwelveDaily.Api.Dtos;
using TwelveDaily.Api.Models;

namespace TwelveDaily.Api.Controllers;


[ApiController]
[Route("api/v1/usuarios")] // [Route("api/[controller]")]
public class UsuarioController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsuarioController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet] // GET /api/usuario
    public async Task<IActionResult> GetTodosUsuarios()
    {
        var usuarios = await _context.Usuarios.ToListAsync();
        return Ok(usuarios);
    }

    [HttpGet("{id}")] // GET /api/usuario/{id}
    public async Task<IActionResult> GetUsuarioPorID(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);

        if (usuario == null)
            return NotFound();

        return Ok(usuario);
    }

    [HttpPost] // POST /api/usuario
    public async Task<IActionResult> CadastraUsuario([FromBody] UsuarioCreateDto usuarioDto)
    {

        ValidaUsuario(usuarioDto); // Validando entrada.

        string senhaHash = GerarHash(usuarioDto.Senha);

        Usuario usuario = new Usuario
        {
            Nome = usuarioDto.Nome,
            Email = usuarioDto.Email,
            SenhaHash = senhaHash
        };

        _context.Usuarios.Add(usuario); // Marcando para inserção
        await _context.SaveChangesAsync(); // Salvando as alterações no banco

        return CreatedAtAction(
            nameof(GetUsuarioPorID),
            new { id = usuario.Id },
            usuario
        );
    }

    [HttpPut("{id}")] // PUT /api/usuario/{id}
    public async Task<IActionResult> AtualizaUsuario(int id, UsuarioCreateDto usuarioAtualizado)
    {
        try
        {
            ValidaUsuario(usuarioAtualizado); // Validando entrada.

            var usuarioAtual = await _context.Usuarios.FindAsync(id);

            if (usuarioAtual == null)
                return NotFound("Usuário não encontrado");

            string senhaHash = GerarHash(usuarioAtualizado.Senha);

            usuarioAtual.Nome = usuarioAtualizado.Nome;
            usuarioAtual.Email = usuarioAtualizado.Email;
            usuarioAtual.SenhaHash = senhaHash;

            await _context.SaveChangesAsync();

            return Ok(usuarioAtual);
        }
        catch (System.Exception exception)
        {
            return BadRequest("Erro na requisição: " + exception.Message);
        }
    }

    [HttpDelete("{id}")] // DELETE /api/usuario/{id}
    public async Task<ActionResult> DeletaUsuario(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);

        if (usuario == null)
            return NotFound("Usuário não encontrado");

        _context.Usuarios.Remove(usuario);
        await _context.SaveChangesAsync();

        return NoContent(); // 204
    }

    private static string GerarHash(string senha)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(senha);
        var hash = sha256.ComputeHash(bytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    private static void ValidaUsuario(UsuarioCreateDto usuarioDto)
    {
        if (string.IsNullOrWhiteSpace(usuarioDto.Nome))
        {
            throw new Exception("Nome é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(usuarioDto.Email))
        {
            throw new Exception("Email é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(usuarioDto.Senha))
        {
            throw new Exception("Senha é obrigatório.");
        }
    }

}