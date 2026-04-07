using Serilog;
using Wordki.Bff.SharedKernel.Events;

namespace Wordki.Bff.Api.BackgroundServices;

public sealed class OutboxMessageProcessorHostedService(
    IServiceScopeFactory serviceScopeFactory,
    TimeProvider timeProvider) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 100;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Unexpected error while processing outbox messages.");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var outboxStore = scope.ServiceProvider.GetRequiredService<IOutboxMessageStore>();
        var handlers = scope.ServiceProvider.GetServices<IOutboxMessageHandler>().ToArray();

        var messages = await outboxStore.GetUnprocessedAsync(BatchSize, cancellationToken);
        if (messages.Count == 0)
        {
            return;
        }

        foreach (var message in messages)
        {
            var matching = handlers
                .Where(x => string.Equals(x.EventType, message.DataType, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matching.Count == 0)
            {
                Log.Warning(
                    "Outbox message {MessageId} has no handler for event type '{DataType}'.",
                    message.Id,
                    message.DataType);
                continue;
            }

            try
            {
                foreach (var handler in matching)
                {
                    await handler.HandleAsync(message, cancellationToken);
                }

                await outboxStore.MarkAsHandledAsync(message.Id, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
            }
            catch (Exception exception)
            {
                Log.Error(
                    exception,
                    "Failed to process outbox message {MessageId} (event type '{DataType}').",
                    message.Id,
                    message.DataType);
            }
        }
    }
}
