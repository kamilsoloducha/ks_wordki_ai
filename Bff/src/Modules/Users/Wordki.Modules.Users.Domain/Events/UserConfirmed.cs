namespace Wordki.Modules.Users.Domain.Events;

public sealed class UserConfirmed
{
    public required Guid Id { get; init; }
}
