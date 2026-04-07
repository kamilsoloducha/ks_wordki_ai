namespace Wordki.Modules.Cards.Application.Queries;

/// <summary>
/// Skąd bierzemy słowa do lekcji: powtórki (termin w przeszłości / dziś) albo nauka nowych (brak zaplanowanego <c>next_repeat_utc</c> na stronie pytania).
/// </summary>
public enum LessonWordSource
{
    /// <summary>Powtórka: <c>next_repeat_utc</c> ustawione i przed końcem dziś (UTC).</summary>
    Review = 0,

    /// <summary>Nowe: strona pytania ma <c>next_repeat_utc</c> równe <c>null</c>.</summary>
    NewWords = 1
}
