namespace AIRecruitment.Application.DTOs;

// ============== 
// TV2 — DTO hồ sơ ứng viên và metadata CV.
// ==============
public sealed record CandidateProfileDto(int Id, string Email, string FullName, string? Phone, string? Location, string? Summary, int? YearsExperience, string? SkillsCsv);
public sealed record UpdateCandidateProfileRequest(string FullName, string? Phone, string? Location, string? Summary, int? YearsExperience, string? SkillsCsv);
public sealed record CvDto(int Id, string OriginalFileName, string ContentType, long SizeBytes, DateTime UploadedAtUtc);

// ============== 
// TV3 — DTO hồ sơ Recruiter.
// ==============
public sealed record RecruiterProfileDto(int Id, string Email, string FullName, string? Phone, string? Position);
public sealed record UpdateRecruiterProfileRequest(string FullName, string? Phone, string? Position);
