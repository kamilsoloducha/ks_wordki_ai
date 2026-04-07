using Microsoft.EntityFrameworkCore;
using Wordki.Modules.Cards.Domain.Entities;

namespace Wordki.Modules.Cards.Application.Abstractions;

public interface ICardsDbContext
{
    DbSet<User> Users { get; }
    DbSet<Group> Groups { get; }
    DbSet<Card> Cards { get; }
    DbSet<CardSide> CardSides { get; }
    DbSet<Result> Results { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
