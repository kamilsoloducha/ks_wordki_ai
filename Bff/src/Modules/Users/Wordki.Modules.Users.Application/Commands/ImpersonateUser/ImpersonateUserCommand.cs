using MediatR;

namespace Wordki.Modules.Users.Application.Commands.ImpersonateUser;

public sealed record ImpersonateUserCommand(Guid TargetUserId) : IRequest<ImpersonateUserResult>;

public sealed record ImpersonateUserResult(Guid EffectiveUserId, string AccessToken, DateTime ExpiresAtUtc);

public sealed class ImpersonateUserCommandHandler(TimeProvider timeProvider) : IRequestHandler<ImpersonateUserCommand, ImpersonateUserResult>
{
    public Task<ImpersonateUserResult> Handle(ImpersonateUserCommand request, CancellationToken cancellationToken)
    {
        var result = new ImpersonateUserResult(
            request.TargetUserId,
            "mock-impersonation-token",
            timeProvider.GetUtcNow().AddMinutes(30).UtcDateTime);

        return Task.FromResult(result);
    }
}
