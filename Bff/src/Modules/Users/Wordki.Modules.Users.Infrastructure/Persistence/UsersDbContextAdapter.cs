using Microsoft.EntityFrameworkCore;
using Wordki.Bff.SharedKernel.Events;
using Wordki.Modules.Users.Application.Abstractions;
using Wordki.Modules.Users.Domain.Users;

namespace Wordki.Modules.Users.Infrastructure.Persistence;

internal sealed class UsersDbContextAdapter(UsersDbContext dbContext) : IUsersDbContext
{
    public async Task<IUsersDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        return new UsersDbTransaction(transaction);
    }

    public Task<bool> NormalizedEmailExistsAsync(NormalizedEmail normalizedEmail, CancellationToken cancellationToken)
    {
        return dbContext.Users.AnyAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public Task<User?> GetByNormalizedEmailAsync(NormalizedEmail normalizedEmail, CancellationToken cancellationToken)
    {
        return dbContext.Users.SingleOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public Task<User?> GetByEmailConfirmationTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return dbContext.Users.SingleOrDefaultAsync(x => x.EmailConfirmationTokenHash == tokenHash, cancellationToken);
    }

    public Task AddUserAsync(User user, CancellationToken cancellationToken)
    {
        return dbContext.Users.AddAsync(user, cancellationToken).AsTask();
    }

    public Task AddSharedEventMessagesAsync(IReadOnlyCollection<SharedEventMessage> messages, CancellationToken cancellationToken)
    {
        return dbContext.SharedEventMessages.AddRangeAsync(messages, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
