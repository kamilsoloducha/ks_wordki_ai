namespace Wordki.Bff.SharedKernel.Events;

public interface IOutboxMessageHandler
{
    /// <summary>Klucz typu zdarzenia — musi odpowiadać <see cref="SharedEventMessage.DataType"/>.</summary>
    string EventType { get; }

    /// <summary>Nazwa handlera (np. moduł) — tylko do logów i diagnostyki.</summary>
    string HandlerName { get; }

    Task HandleAsync(SharedEventMessage message, CancellationToken cancellationToken);
}

