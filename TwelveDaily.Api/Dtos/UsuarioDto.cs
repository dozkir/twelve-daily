namespace TwelveDaily.Api.Dtos;

public record UsuarioCreateDto(string Nome, string Email, string Senha); // Usado para criação e alteração de usuário

public record UsuarioLoginDto(string Email, string Senha);