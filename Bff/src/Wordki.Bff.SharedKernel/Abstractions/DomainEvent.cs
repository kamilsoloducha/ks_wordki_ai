namespace Wordki.Bff.SharedKernel.Abstractions;

public abstract record DomainEvent(Guid EventId, DateTime OccurredOnUtc);
