namespace Wordki.Modules.Users.Application.Abstractions;

public interface IConfirmationTokenHasher
{
    string Hash(string token);
}
