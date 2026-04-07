namespace Wordki.Bff.SharedKernel.Events;

/// <summary>
/// Zapisuje wiadomości do wspólnego outboxa (schema <c>users</c>) i zatwierdza transakcję.
/// Używane przez moduły bez własnej tabeli outbox.
/// </summary>
public interface ISharedEventOutboxWriter
{
    Task AddAndSaveAsync(IReadOnlyCollection<SharedEventMessage> messages, CancellationToken cancellationToken);
}
