namespace Wordki.Modules.Cards.Domain.Entities;

public sealed class Result
{
    public const int MaxDrawer = 5;

    public long Id { get; init; }
    public long UserId { get; init; }
    public long GroupId { get; init; }
    public long CardSideId { get; init; }
    public int Drawer { get; private set; }
    /// <summary>
    /// Term następnej powtórki (UTC). <c>null</c> — strona nie jest brana pod uwagę przy układaniu lekcji (dopóki nie zostanie ustawiona).
    /// </summary>
    public DateTime? NextRepeatUtc { get; private set; }
    public int Counter { get; private set; }
    public User User { get; init; } = null!;
    public CardSide CardSide { get; init; } = null!;

    /// <summary>
    /// Nowy wynik SRS dla strony karty: szuflada 0, licznik 0. Domyślnie <see cref="NextRepeatUtc"/> = <c>null</c> (nie wchodzi w lekcję do pierwszej oceny).
    /// </summary>
    public static Result CreateInitial(
        long userId,
        long groupId,
        long cardSideId,
        DateTime? nextRepeatUtc = null)
    {
        return new Result
        {
            UserId = userId,
            GroupId = groupId,
            CardSideId = cardSideId,
            Drawer = 0,
            Counter = 0,
            NextRepeatUtc = nextRepeatUtc
        };
    }

    public void RegisterSuccess(DateTime nextRepeatUtc)
    {
        Drawer = Math.Min(MaxDrawer, Drawer + 1);
        Counter++;
        NextRepeatUtc = nextRepeatUtc;
    }

    public void RegisterFailure(DateTime nextRepeatUtc)
    {
        Drawer = Math.Max(0, Drawer - 1);
        Counter++;
        NextRepeatUtc = nextRepeatUtc;
    }
}
