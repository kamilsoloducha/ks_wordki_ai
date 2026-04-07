using MediatR;

namespace Wordki.Modules.Users.Application.Commands.RemoveCurrentUser;

public sealed record RemoveCurrentUserCommand : IRequest;

public sealed class RemoveCurrentUserCommandHandler : IRequestHandler<RemoveCurrentUserCommand>
{
    public Task Handle(RemoveCurrentUserCommand request, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
