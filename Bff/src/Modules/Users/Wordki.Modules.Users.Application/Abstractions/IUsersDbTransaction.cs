namespace Wordki.Modules.Users.Application.Abstractions;

public interface IUsersDbTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
    Task RollbackAsync(CancellationToken cancellationToken);
}
