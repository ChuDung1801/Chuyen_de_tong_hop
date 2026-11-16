namespace AIRecruitment.Application.DTOs;

// ============== 
// TV3 — DTO cho Company/Job API, tránh trả entity EF trực tiếp.
// ==============
public sealed record CompanyDto(int Id, string Name, string? Website, string? Industry, string? Description, string? Location);
public sealed record CreateCompanyRequest(string Name, string? Website, string? Industry, string? Description, string? Location);
public sealed record JobResponseDto(int Id, int? CompanyId, string Title, string Description, string Location, string EmploymentType, string? RequiredSkillsCsv, DateTime CreatedAtUtc, bool IsPublished);
public sealed record CreateRecruitmentJobRequest(int? CompanyId, string Title, string Description, string Location, string EmploymentType, string? RequiredSkillsCsv, bool IsPublished = true);
