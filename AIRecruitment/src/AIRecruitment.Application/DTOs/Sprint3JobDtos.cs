using System.ComponentModel.DataAnnotations;
namespace AIRecruitment.Application.DTOs;

// ============== 
// TV3 — Search/filter/pagination and recruiter CRUD contracts.
// ==============
public sealed record JobSearchPageDto(IReadOnlyList<JobResponseDto> Items, int Page, int PageSize, int TotalCount, int TotalPages);
public sealed record UpdateRecruitmentJobRequest(int? CompanyId, [Required, StringLength(200, MinimumLength=2)] string Title, [Required, StringLength(8000, MinimumLength=10)] string Description, [StringLength(200)] string Location, [StringLength(50)] string EmploymentType, [StringLength(2000)] string? RequiredSkillsCsv, bool IsPublished);
public sealed record SavedJobDto(int JobId, string Title, string Location, string EmploymentType, DateTime SavedAtUtc);
