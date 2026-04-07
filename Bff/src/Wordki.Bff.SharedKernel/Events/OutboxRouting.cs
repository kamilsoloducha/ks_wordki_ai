namespace Wordki.Bff.SharedKernel.Events;

/// <summary>
/// Konwencje routingu wiadomości outbox: jeden rekord na zdarzenie; wszyscy handlerzy
/// z tym samym <see cref="IOutboxMessageHandler.EventType"/> co <see cref="SharedEventMessage.DataType"/> są wywoływani.
/// </summary>
public static class OutboxRouting
{
    /// <summary>
    /// Wartość <see cref="SharedEventMessage.ConsumerName"/> gdy zdarzenie jest publikowane do wszystkich zainteresowanych handlerów (bez wskazywania konkretnego odbiorcy).
    /// </summary>
    public const string Broadcast = "*";
}
