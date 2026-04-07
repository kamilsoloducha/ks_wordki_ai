using Wordki.Modules.Cards.Api.Responses;

namespace Wordki.Modules.Cards.Api.Requests;

public sealed record CreateCardGroupRequest(Guid UserId, string Name, string FrontSideType, string BackSideType);

public sealed record AddCardToGroupRequest(
    Guid UserId,
    long GroupId,
    string FrontLabel,
    string BackLabel,
    string? FrontExample = null,
    string? FrontComment = null,
    string? BackExample = null,
    string? BackComment = null);

public sealed record UpdateCardGroupRequest(
    Guid UserId,
    string Name,
    string FrontSideType,
    string BackSideType);

public sealed record UpdateCardRequest(Guid UserId, CardSideDto Front, CardSideDto Back);
