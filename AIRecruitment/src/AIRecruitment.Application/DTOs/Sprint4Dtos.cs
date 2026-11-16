namespace AIRecruitment.Application.DTOs;

// ============== 
// TV4 — DTO ứng tuyển, trạng thái và lịch phỏng vấn; không trả đường dẫn CV vật lý.
// ==============
public sealed record ApplyJobRequest(int? CVId, string? CoverLetter);
public sealed record UpdateApplicationStatusRequest(string Status);
public sealed record ApplicationDto(int Id, int CandidateProfileId, int JobId, string JobTitle, string CandidateName, string CandidateEmail, int? CVId, string? CVFileName, string? CoverLetter, string Status, DateTime AppliedAtUtc, DateTime UpdatedAtUtc);
public sealed record CreateInterviewRequest(DateTime ScheduledAtUtc, int DurationMinutes = 60, string? MeetingUrl = null, string? Location = null, string? Notes = null);
public sealed record InterviewDto(int Id, int ApplicationId, DateTime ScheduledAtUtc, int DurationMinutes, string? MeetingUrl, string? Location, string? Notes, string Status, string JobTitle, string CandidateName);
public sealed record CVAnalysisDto(long Id, int CVId, string Status, IReadOnlyList<string> ExtractedSkills, int? YearsExperience, string? Education, string? ErrorCode, string ParserVersion, DateTime CreatedAtUtc, DateTime? CompletedAtUtc);
public sealed record MatchResultDto(long Id, int CVId, int JobId, string JobTitle, decimal MatchScore, IReadOnlyList<string> MatchingSkills, IReadOnlyList<string> MissingSkills, string? Explanation, string ModelVersion, DateTime CreatedAtUtc);
public sealed record CVAnalysisServiceResponse(IReadOnlyList<string> Skills, int? YearsExperience, string? Education, string ParserVersion, string ExtractedTextPreview);
public sealed record MatchingServiceRequest(IReadOnlyList<string> CvSkills, int CvYearsExperience, string CvText, string JobTitle, string JobDescription, string? RequiredSkillsCsv, int? RequiredYearsExperience = null, string? CvTitle = null);
public sealed record MatchingServiceResponse(decimal MatchScore, IReadOnlyList<string> MatchingSkills, IReadOnlyList<string> MissingSkills, string Explanation, string ModelVersion, IReadOnlyDictionary<string, double> FeatureValues, string Mode);

// TV5 — Request/response cho việc xếp hạng các tin đang tuyển theo CV.
public sealed record RecommendationJobRequest(int JobId, string JobTitle, string JobDescription, string? RequiredSkillsCsv, int? RequiredYearsExperience, string Location, string EmploymentType);
public sealed record RecommendationServiceRequest(IReadOnlyList<string> CvSkills, int CvYearsExperience, string CvText, string? CvTitle, IReadOnlyList<RecommendationJobRequest> Jobs, int TopK = 5);
public sealed record RecommendationServiceItem(int JobId, string JobTitle, string Location, string EmploymentType, decimal MatchScore, IReadOnlyList<string> MatchingSkills, IReadOnlyList<string> MissingSkills, string Explanation, string ModelVersion, IReadOnlyDictionary<string, double> FeatureValues, string Mode);
public sealed record RecommendationServiceResponse(IReadOnlyList<RecommendationServiceItem> Recommendations);
public sealed record RecommendedJobDto(int Rank, long MatchResultId, int CVId, int JobId, string JobTitle, string Location, string EmploymentType, decimal MatchScore, IReadOnlyList<string> MatchingSkills, IReadOnlyList<string> MissingSkills, string Explanation, string ModelVersion, string Mode, DateTime CreatedAtUtc);
