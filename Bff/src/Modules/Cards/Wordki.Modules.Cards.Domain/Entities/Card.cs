namespace Wordki.Modules.Cards.Domain.Entities;

public sealed class Card
{
    public long Id { get; init; }
    public long GroupId { get; init; }
    public long FrontSideId { get; init; }
    public long BackSideId { get; init; }
    public Group Group { get; init; } = null!;
    public CardSide FrontSide { get; init; } = null!;
    public CardSide BackSide { get; init; } = null!;
}
