namespace Wordki.Bff.SharedKernel.Events;

public interface IOutboxMessageStore
{
    Task<IReadOnlyCollection<SharedEventMessage>> GetUnprocessedAsync(int batchSize, CancellationToken cancellationToken);
    Task MarkAsHandledAsync(Guid messageId, DateTime handledAtUtc, CancellationToken cancellationToken);
}
