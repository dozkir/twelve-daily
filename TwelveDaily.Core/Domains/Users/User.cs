using System.Security.Cryptography;
using System.Text;

namespace TwelveDaily.Core.Domains.Users;

public class User
{
    public int Id { get; }
    public string Name { get; set;  }
    public string Email { get; set;  }
    public string HashedPassword { get; private set; }
    public DateTime CreatedAt { get; }

    
    public User(string name, string email, string plainTextPassword)
    {
        Name = name;
        Email = email;
        CreatePasswordHash(plainTextPassword);
    }

    private void CreatePasswordHash(string rawPassword)
    {
        // Creating hash only if HashedPassword is actually null. Preventing it from unwanted rewrite.
        if (HashedPassword == null)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(rawPassword);
            var hash = sha256.ComputeHash(bytes);
            HashedPassword = BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }

    // public sealed override void Validate()
    // {
    //     if (Name == null)
    //     {
    //         RegisterError("Name is required");
    //     }
    //
    //     if (Email == null)
    //     {
    //         RegisterError("Email is required");
    //     }
    // }
}

