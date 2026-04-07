namespace Wordki.Bff.SharedKernel.Events;

/// <summary>
/// Zdarzenie integracyjne: użytkownik udzielił odpowiedzi w lekcji (powtórka wpisana po stronie modułu Lessons).
/// Moduł Cards aktualizuje SRS (<see cref="Result"/>) dla wskazanego wyniku.
/// </summary>
public sealed record RepeatAddedIntegrationEvent(long ResultId, bool Result, Guid UserId);
