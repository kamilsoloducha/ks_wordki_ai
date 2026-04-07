namespace Wordki.Bff.SharedKernel.Events;

public sealed class SharedEventMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string PublisherName { get; init; } = string.Empty;

    /// <summary>
    /// Dla zdarzeń broadcast używaj <see cref="OutboxRouting.Broadcast"/>.
    /// Routing do handlerów odbywa się po <see cref="DataType"/>, nie po tej kolumnie.
    /// </summary>
    public string ConsumerName { get; init; } = string.Empty;

    /// <summary>Typ zdarzenia (klucz) — powiązany z <see cref="IOutboxMessageHandler.EventType"/>.</summary>
    public string DataType { get; init; } = string.Empty;
    public DateTime AddedAtUtc { get; init; }
    public DateTime? HandledAtUtc { get; set; }
    public string Payload { get; init; } = string.Empty;
}