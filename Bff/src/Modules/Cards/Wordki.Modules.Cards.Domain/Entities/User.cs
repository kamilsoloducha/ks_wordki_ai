namespace Wordki.Modules.Cards.Domain.Entities;

public sealed class User
{
    public long Id { get; init; }
    public Guid ExternalUserId { get; init; }
    public List<Group> Groups { get; init; } = [];
    public List<Result> Results { get; init; } = [];
}
