using Microsoft.EntityFrameworkCore;
using Wordki.Modules.Lessons.Domain.Entities;

namespace Wordki.Modules.Lessons.Application.Abstractions;

public interface ILessonsDbContext
{
    DbSet<User> Users { get; }
    DbSet<Lesson> Lessons { get; }
    DbSet<LessonRepetition> LessonRepetitions { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
