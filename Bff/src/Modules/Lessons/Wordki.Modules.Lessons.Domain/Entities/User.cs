namespace Wordki.Modules.Lessons.Domain.Entities;

/// <summary>
/// Użytkownik modułu Lessons — wewnętrzne Id oraz ExternalUserId spójne z modułem Cards.
/// </summary>
public sealed class User
{
    public long Id { get; init; }
    public Guid ExternalUserId { get; init; }
    public List<Lesson> Lessons { get; init; } = [];
}
