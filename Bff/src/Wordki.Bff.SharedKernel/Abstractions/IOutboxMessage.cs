namespace Wordki.Bff.SharedKernel.Abstractions;

public interface IOutboxMessage
{
    Guid Id { get; }
    string Type { get; }
    string Payload { get; }
    DateTime OccurredOnUtc { get; }
}
