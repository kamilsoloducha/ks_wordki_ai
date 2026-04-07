using Microsoft.EntityFrameworkCore;
using Wordki.Bff.SharedKernel.Events;

namespace Wordki.Modules.Users.Infrastructure.Persistence;

internal sealed class SharedEventOutboxWriter(UsersDbContext dbContext) : ISharedEventOutboxWriter
{
    public async Task AddAndSaveAsync(IReadOnlyCollection<SharedEventMessage> messages, CancellationToken cancellationToken)
    {
        await dbContext.SharedEventMessages.AddRangeAsync(messages, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
