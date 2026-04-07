using Microsoft.EntityFrameworkCore;
using Wordki.Bff.SharedKernel.Events;

namespace Wordki.Modules.Users.Infrastructure.Persistence;

internal sealed class UsersOutboxMessageStore(UsersDbContext dbContext) : IOutboxMessageStore
{
    public async Task<IReadOnlyCollection<SharedEventMessage>> GetUnprocessedAsync(int batchSize, CancellationToken cancellationToken)
    {
        return await dbContext.SharedEventMessages
            .Where(x => x.HandledAtUtc == null)
            .OrderBy(x => x.AddedAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsHandledAsync(Guid messageId, DateTime handledAtUtc, CancellationToken cancellationToken)
    {
        var message = await dbContext.SharedEventMessages
            .SingleOrDefaultAsync(x => x.Id == messageId, cancellationToken);

        if (message is null)
        {
            return;
        }

        message.HandledAtUtc = handledAtUtc;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
