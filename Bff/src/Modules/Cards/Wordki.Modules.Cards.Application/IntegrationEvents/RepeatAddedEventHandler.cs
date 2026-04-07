using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Wordki.Bff.SharedKernel.Events;
using Wordki.Modules.Cards.Application.Abstractions;
using Wordki.Modules.Cards.Application.Srs;
using Wordki.Modules.Cards.Domain.Entities;

namespace Wordki.Modules.Cards.Application.IntegrationEvents;

public sealed class RepeatAddedEventHandler(ICardsDbContext dbContext, TimeProvider timeProvider)
    : IOutboxMessageHandler
{
    public string HandlerName => "Cards";
    public string EventType => "RepeatAdded";

    public async Task HandleAsync(SharedEventMessage message, CancellationToken cancellationToken)
    {
        var integrationEvent = JsonSerializer.Deserialize<RepeatAddedIntegrationEvent>(message.Payload);
        if (integrationEvent is null)
        {
            throw new InvalidOperationException($"Cannot deserialize payload for message {message.Id}.");
        }

        var cardsUserId = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.ExternalUserId == integrationEvent.UserId)
            .Select(x => x.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (cardsUserId == 0)
        {
            Log.Warning(
                "RepeatAdded: cards user {UserId} not found; skipping result {ResultId}.",
                integrationEvent.UserId,
                integrationEvent.ResultId);
            return;
        }

        var result = await dbContext.Results
            .FirstOrDefaultAsync(
                x => x.Id == integrationEvent.ResultId && x.UserId == cardsUserId,
                cancellationToken);

        if (result is null)
        {
            Log.Warning(
                "RepeatAdded: result {ResultId} not found for user {UserId}.",
                integrationEvent.ResultId,
                integrationEvent.UserId);
            return;
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        if (integrationEvent.Result)
        {
            var drawerAfter = Math.Min(Result.MaxDrawer, result.Drawer + 1);
            var next = NextRepeatCalculator.ComputeNextRepeatUtc(nowUtc, drawerAfter);
            result.RegisterSuccess(next);
        }
        else
        {
            var drawerAfter = Math.Max(0, result.Drawer - 1);
            var next = NextRepeatCalculator.ComputeNextRepeatUtc(nowUtc, drawerAfter);
            result.RegisterFailure(next);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        Log.Information(
            "Updated SRS for result {ResultId} (known: {Known}, drawer now {Drawer}).",
            result.Id,
            integrationEvent.Result,
            result.Drawer);
    }
}
