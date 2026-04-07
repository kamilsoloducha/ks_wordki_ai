using MediatR;
using Microsoft.EntityFrameworkCore;
using Wordki.Bff.SharedKernel.Results;
using Wordki.Modules.Cards.Application.Abstractions;

namespace Wordki.Modules.Cards.Application.Queries.GetDistinctSideTypePairs;

/// <summary>
/// Unordered pair of side types from the user's groups: (A,B) with A &lt;= B lexicographically,
/// so (EN,PL) and (PL,EN) collapse to one item.
/// </summary>
public sealed record GetDistinctSideTypePairsQuery(Guid UserId)
    : IRequest<Result<IReadOnlyList<SideTypePairItem>>>;

public sealed record SideTypePairItem(string SideType1, string SideType2);

public sealed class GetDistinctSideTypePairsQueryHandler(ICardsDbContext dbContext)
    : IRequestHandler<GetDistinctSideTypePairsQuery, Result<IReadOnlyList<SideTypePairItem>>>
{
    public async Task<Result<IReadOnlyList<SideTypePairItem>>> Handle(
        GetDistinctSideTypePairsQuery request,
        CancellationToken cancellationToken)
    {
        var validation = GetDistinctSideTypePairsQueryValidator.Validate(request);
        if (validation.IsFailure)
        {
            return Result<IReadOnlyList<SideTypePairItem>>.Failure(validation.Errors);
        }

        var cardsUserId = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.ExternalUserId == request.UserId)
            .Select(x => x.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (cardsUserId == 0)
        {
            return Result<IReadOnlyList<SideTypePairItem>>.Failure(new AppError(
                "cards.get_side_type_pairs.user.not_found",
                "User was not found in the cards module.",
                ErrorType.NotFound,
                "userId"));
        }

        var rows = await dbContext.Groups
            .AsNoTracking()
            .Where(g => g.UserId == cardsUserId)
            .Select(g => new { g.FrontSideType, g.BackSideType })
            .ToListAsync(cancellationToken);

        var distinct = new HashSet<(string A, string B)>(PairComparer.Instance);
        foreach (var r in rows)
        {
            distinct.Add(CanonicalPair(r.FrontSideType, r.BackSideType));
        }

        var items = distinct
            .OrderBy(p => p.A, StringComparer.Ordinal)
            .ThenBy(p => p.B, StringComparer.Ordinal)
            .Select(p => new SideTypePairItem(p.A, p.B))
            .ToList();

        return Result<IReadOnlyList<SideTypePairItem>>.Success(items);
    }

    private static (string A, string B) CanonicalPair(string x, string y) =>
        string.CompareOrdinal(x, y) <= 0 ? (x, y) : (y, x);

    private sealed class PairComparer : IEqualityComparer<(string A, string B)>
    {
        public static readonly PairComparer Instance = new();

        public bool Equals((string A, string B) x, (string A, string B) y) =>
            string.Equals(x.A, y.A, StringComparison.Ordinal)
            && string.Equals(x.B, y.B, StringComparison.Ordinal);

        public int GetHashCode((string A, string B) obj) =>
            HashCode.Combine(
                StringComparer.Ordinal.GetHashCode(obj.A),
                StringComparer.Ordinal.GetHashCode(obj.B));
    }
}
