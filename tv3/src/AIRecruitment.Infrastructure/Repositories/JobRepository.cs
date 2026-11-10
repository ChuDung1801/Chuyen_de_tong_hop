using AIRecruitment.Application.Interfaces;
using AIRecruitment.Domain.Entities;
using AIRecruitment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruitment.Infrastructure.Repositories;

public sealed class JobRepository(ApplicationDbContext db) : IJobRepository
{
    public async Task<IReadOnlyList<Job>> GetPublishedAsync(CancellationToken cancellationToken = default) =>
        await db.Jobs.AsNoTracking()
            .Where(x => x.IsPublished)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<Job?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        db.Jobs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Job> AddAsync(Job job, CancellationToken cancellationToken = default)
    {
        db.Jobs.Add(job);
        await db.SaveChangesAsync(cancellationToken);
        return job;
    }
}
