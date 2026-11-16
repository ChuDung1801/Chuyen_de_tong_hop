using AIRecruitment.Application.DTOs;
namespace AIRecruitment.Application.Interfaces;

// ============== 
// TV3 — Advanced job search and ownership-aware management.
// ==============
public interface ISprint3JobService
{
    Task<JobSearchPageDto> SearchAsync(string? search, string? location, string? employmentType, string? skill, int page, int pageSize, CancellationToken ct);
    Task<JobResponseDto?> GetPublicAsync(int id, CancellationToken ct);
    Task<JobResponseDto> CreateAsync(int userId, CreateRecruitmentJobRequest request, CancellationToken ct);
    Task<JobResponseDto?> UpdateAsync(int userId, bool isAdmin, int id, UpdateRecruitmentJobRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(int userId, bool isAdmin, int id, CancellationToken ct);
    Task<IReadOnlyList<JobResponseDto>> GetMineAsync(int userId, bool isAdmin, CancellationToken ct);
}

// ============== 
// TV2 — Candidate saved jobs.
// ==============
public interface ISavedJobService
{
    Task<IReadOnlyList<SavedJobDto>> GetMineAsync(int userId, CancellationToken ct);
    Task<bool> SaveAsync(int userId, int jobId, CancellationToken ct);
    Task<bool> RemoveAsync(int userId, int jobId, CancellationToken ct);
}
