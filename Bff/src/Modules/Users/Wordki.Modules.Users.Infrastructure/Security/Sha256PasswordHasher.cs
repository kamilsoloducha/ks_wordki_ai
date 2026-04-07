using System.Security.Cryptography;
using System.Text;
using Wordki.Modules.Users.Application.Abstractions;

namespace Wordki.Modules.Users.Infrastructure.Security;

public sealed class Sha256PasswordHasher : IPasswordHasher
{
    public string HashPassword(string plainPassword)
    {
        var bytes = Encoding.UTF8.GetBytes(plainPassword);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
