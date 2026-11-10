using AIRecruitment.Domain.Entities;

namespace AIRecruitment.Application.Interfaces;

public interface IJobRepository
{
    Task<IReadOnlyList<Job>> GetPublishedAsync(CancellationToken cancellationToken = default);
    Task<Job?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Job> AddAsync(Job job, CancellationToken cancellationToken = default);
}
