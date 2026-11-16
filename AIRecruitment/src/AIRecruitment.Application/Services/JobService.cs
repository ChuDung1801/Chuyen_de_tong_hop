using AIRecruitment.Application.DTOs;
using AIRecruitment.Application.Interfaces;
using AIRecruitment.Domain.Entities;

namespace AIRecruitment.Application.Services;

public sealed class JobService(IJobRepository repository)
{
    public async Task<IReadOnlyList<JobDto>> GetPublishedAsync(CancellationToken ct = default)
    {
        var jobs = await repository.GetPublishedAsync(ct);
        return jobs.Select(ToDto).ToArray();
    }

    public async Task<JobDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var job = await repository.GetByIdAsync(id, ct);
        return job is null ? null : ToDto(job);
    }

    public async Task<JobDto> CreateAsync(CreateJobRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Job title is required.");
        if (string.IsNullOrWhiteSpace(request.Description))
            throw new ArgumentException("Job description is required.");

        var job = new Job
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Location = request.Location?.Trim() ?? string.Empty,
            EmploymentType = string.IsNullOrWhiteSpace(request.EmploymentType) ? "FullTime" : request.EmploymentType.Trim(),
            IsPublished = false
        };
        return ToDto(await repository.AddAsync(job, ct));
    }

    private static JobDto ToDto(Job j) => new(j.Id, j.Title, j.Description, j.Location, j.EmploymentType, j.IsPublished);
}
