using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Wordki.Bff.SharedKernel.Events;
using Wordki.Modules.Cards.Application.Abstractions;
using Wordki.Modules.Cards.Domain.Entities;

namespace Wordki.Modules.Cards.Application.IntegrationEvents;

public sealed class UserConfirmedEventHandler(ICardsDbContext dbContext) : IOutboxMessageHandler
{
    public string HandlerName => "Cards";
    public string EventType => "UserConfirmed";

    public async Task HandleAsync(SharedEventMessage message, CancellationToken cancellationToken)
    {
        var integrationEvent = JsonSerializer.Deserialize<UserConfirmedIntegrationEvent>(message.Payload);
        if (integrationEvent is null)
        {
            throw new InvalidOperationException($"Cannot deserialize payload for message {message.Id}.");
        }

        var userExists = await dbContext.Users
            .AnyAsync(x => x.ExternalUserId == integrationEvent.Id, cancellationToken);

        if (userExists)
        {
            Log.Information(
                "Cards user {UserId} already exists. Skipping creation.",
                integrationEvent.Id);
            return;
        }

        var user = new User
        {
            ExternalUserId = integrationEvent.Id
        };

        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        Log.Information(
            "Created Cards user {UserId} from UserConfirmed event.",
            integrationEvent.Id);
    }
}
