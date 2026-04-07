namespace Wordki.Modules.Cards.Application.Srs;

/// <summary>
/// Wylicza termin następnej powtórki na podstawie szuflady po aktualizacji — większa szuflada ⇒ dłuższy odstęp.
/// </summary>
public static class NextRepeatCalculator
{
    /// <param name="nowUtc">Aktualny moment (UTC).</param>
    /// <param name="drawerAfterUpdate">Wartość szuflady po zastosowaniu sukcesu lub porażki.</param>
    public static DateTime ComputeNextRepeatUtc(DateTime nowUtc, int drawerAfterUpdate)
    {
        return nowUtc.Add(IntervalForDrawer(drawerAfterUpdate));
    }

    private static TimeSpan IntervalForDrawer(int drawer)
    {
        if (drawer <= 0)
        {
            return TimeSpan.FromHours(4);
        }

        // Wykładniczo w dniach: 1, 2, 4, 8, … (szuflada 1 → 1 dzień, 2 → 2 dni, …)
        var days = Math.Pow(2, Math.Min(drawer - 1, 8));
        return TimeSpan.FromDays(days);
    }
}
