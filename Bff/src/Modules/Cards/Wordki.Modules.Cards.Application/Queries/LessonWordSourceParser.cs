namespace Wordki.Modules.Cards.Application.Queries;

public static class LessonWordSourceParser
{
    /// <summary>
    /// Parametr query: <c>review</c> (domyślnie) lub <c>newWords</c>.
    /// </summary>
    public static LessonWordSource Parse(string? wordSource)
    {
        if (string.IsNullOrWhiteSpace(wordSource))
        {
            return LessonWordSource.Review;
        }

        return wordSource.Trim().Equals("newWords", StringComparison.OrdinalIgnoreCase)
            ? LessonWordSource.NewWords
            : LessonWordSource.Review;
    }
}
