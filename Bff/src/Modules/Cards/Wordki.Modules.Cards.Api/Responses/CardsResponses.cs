namespace Wordki.Modules.Cards.Api.Responses;

public sealed record CardGroupDto(long Id, string Name, string FrontSideType, string BackSideType);

public sealed record UserCardGroupDto(long Id, string Name, string FrontSideType, string BackSideType, int CardCount);

public sealed record CardDto(
    long Id,
    long GroupId,
    CardSideDto Front,
    CardSideDto Back,
    long? QuestionResultId = null);

public sealed record CardSideDto(string Label, string Example, string Comment);

/// <summary>
/// Słowo w lekcji: strona pytania (z wynikiem SRS) i strona odpowiedzi wg wybranego kierunku.
/// </summary>
public sealed record LessonWordDto(
    long QuestionResultId,
    int QuestionDrawer,
    string QuestionLabel,
    string QuestionExample,
    string AnswerLabel,
    string AnswerExample);

public sealed record UserWordCountDto(int WordCount);

public sealed record WordsDueTodayCountDto(int DueTodayCount);

public sealed record SearchCardsCountDto(int Count);

public sealed record SearchCardsWithCountDto(int Count, IReadOnlyList<CardDto> Items);

/// <summary>
/// Unordered language/side pair from the user's groups; <see cref="SideType1"/> &lt;= <see cref="SideType2"/> (ordinal).
/// </summary>
public sealed record SideTypePairDto(string SideType1, string SideType2);
