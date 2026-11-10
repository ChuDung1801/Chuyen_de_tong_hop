namespace AIRecruitment.Application.DTOs;

public sealed record JobDto(int Id, string Title, string Description, string Location, string EmploymentType, bool IsPublished);
public sealed record CreateJobRequest(string Title, string Description, string Location, string EmploymentType);
public sealed record MatchRequest(int JobId, string CvText, string[] Skills);
public sealed record MatchResponse(decimal MatchScore, string ModelVersion, string[] MatchingSkills, string[] MissingSkills, string Explanation);
