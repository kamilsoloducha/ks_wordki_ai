namespace Wordki.Modules.Users.Application.Abstractions;

public interface IPasswordHasher
{
    string HashPassword(string plainPassword);
}
