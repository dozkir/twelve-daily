using Microsoft.AspNetCore.Identity;

namespace TwelveDaily.Api.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty; // Hash da senha.
    public DateTime DataCriacao { get; set; }

    public static Usuario test()
    {
        return new Usuario
        {
            Id = 1,
            Nome = "Hello",
            Email = "hello@example.com",
            SenhaHash = "iuhfvvhgfgyf"
        };
    }

    /*
    public static User Create(string nome, string email, string senha)
    {
        try
        {
            // Validações básicas no nome, email e senha.

            // Criar usuário no banco de dados.

            var hasher = new PasswordHasher<Usuario>();

            string senhaHash = hasher.HashPassword();


            return new User
            {
                Nome = nome,
                Email = email,
                Senha = senha,
                DataCriacao = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            throw new Exception("Erro ao criar usuário: " + ex.Message);
        }
    }
    */

}

