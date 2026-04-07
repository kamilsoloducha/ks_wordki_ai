using System.Security.Cryptography;
using System.Text;
using Wordki.Modules.Users.Application.Abstractions;

namespace Wordki.Modules.Users.Application.Security;

public sealed class Sha256ConfirmationTokenHasher : IConfirmationTokenHasher
{
    public string Hash(string token)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var hashBytes = SHA256.HashData(tokenBytes);
        return Convert.ToHexString(hashBytes);
    }
}
