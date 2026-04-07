using MediatR;

namespace Wordki.Modules.Users.Application.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery : IRequest<GetCurrentUserResult>;

public sealed record GetCurrentUserResult(Guid Id, string Email, string Role, string Status);

public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, GetCurrentUserResult>
{
    public Task<GetCurrentUserResult> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var result = new GetCurrentUserResult(
            Guid.NewGuid(),
            "user@example.com",
            "User",
            "Active");

        return Task.FromResult(result);
    }
}
