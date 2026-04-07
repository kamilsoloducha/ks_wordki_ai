using Microsoft.EntityFrameworkCore.Storage;
using Wordki.Modules.Users.Application.Abstractions;

namespace Wordki.Modules.Users.Infrastructure.Persistence;

internal sealed class UsersDbTransaction(IDbContextTransaction transaction) : IUsersDbTransaction
{
    public Task CommitAsync(CancellationToken cancellationToken)
    {
        return transaction.CommitAsync(cancellationToken);
    }

    public Task RollbackAsync(CancellationToken cancellationToken)
    {
        return transaction.RollbackAsync(cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return transaction.DisposeAsync();
    }
}
