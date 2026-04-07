namespace Wordki.Modules.Cards.Domain.Entities;

public sealed class CardSide
{
    public static CardSide Empty => new();

    public long Id { get; init; }
    public string Label { get; set; } = string.Empty;
    public string Example { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    /// <summary>Strona jest przodem karty (jeden do jednego z <see cref="Card.FrontSide"/>).</summary>
    public Card? CardWhereFront { get; set; }
    /// <summary>Strona jest tyłem karty (jeden do jednego z <see cref="Card.BackSide"/>).</summary>
    public Card? CardWhereBack { get; set; }
    public Result? Result { get; set; }
}
