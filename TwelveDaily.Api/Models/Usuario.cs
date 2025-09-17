namespace TwelveDaily.Api.Models;

public class User
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string Email { get; set; }
    public string Senha { get; set; }

    public static User test()
    {
        return new User
        {
            Id = 1,
            Nome = "Hello",
            Email = "hello@example.com",
            Senha = "password"
        };
    }
    
}

