using Wordki.Bff.SharedKernel.Events;
using Wordki.Modules.Users.Domain.Users;

namespace Wordki.Modules.Users.Application.Abstractions;

public interface IUsersDbContext
{
    Task<IUsersDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
    Task<bool> NormalizedEmailExistsAsync(NormalizedEmail normalizedEmail, CancellationToken cancellationToken);
    Task<User?> GetByNormalizedEmailAsync(NormalizedEmail normalizedEmail, CancellationToken cancellationToken);
    Task<User?> GetByEmailConfirmationTokenHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task AddUserAsync(User user, CancellationToken cancellationToken);
    Task AddSharedEventMessagesAsync(IReadOnlyCollection<SharedEventMessage> messages, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
