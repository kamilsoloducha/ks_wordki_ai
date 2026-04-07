namespace Wordki.Modules.Cards.Domain.Entities;

public sealed class Group
{
    public long Id { get; init; }
    public long? UserId { get; init; }
    public string Name { get; set; } = string.Empty;
    public string FrontSideType { get; set; } = string.Empty;
    public string BackSideType { get; set; } = string.Empty;
    public GroupType Type { get; init; } = GroupType.UserOwned;
    public List<Card> Cards { get; init; } = [];
}

public enum GroupType
{
    UserOwned = 1,
    Static = 2
}
