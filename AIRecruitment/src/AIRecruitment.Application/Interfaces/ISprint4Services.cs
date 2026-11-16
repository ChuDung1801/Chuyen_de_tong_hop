using AIRecruitment.Application.DTOs;

namespace AIRecruitment.Application.Interfaces;

// ============== 
// TV4 — Hợp đồng nghiệp vụ cho application và interview.
// ==============
public interface IApplicationService
{
    Task<ApplicationDto> ApplyAsync(int userId, int jobId, ApplyJobRequest request, CancellationToken ct);
    Task<IReadOnlyList<ApplicationDto>> GetMineAsync(int userId, CancellationToken ct);
    Task<IReadOnlyList<ApplicationDto>> GetForJobAsync(int userId, bool isAdmin, int jobId, CancellationToken ct);
    Task<ApplicationDto?> UpdateStatusAsync(int userId, bool isAdmin, int applicationId, string status, CancellationToken ct);
    Task<InterviewDto> CreateInterviewAsync(int userId, bool isAdmin, int applicationId, CreateInterviewRequest request, CancellationToken ct);
    Task<IReadOnlyList<InterviewDto>> GetMyInterviewsAsync(int userId, CancellationToken ct);
}

// ============== 
// TV5 — Adapter để ASP.NET Core không phụ thuộc trực tiếp vào FastAPI.
// ==============
public interface IMLServiceClient
{
    Task<CVAnalysisServiceResponse> AnalyzeCvAsync(string filePath, string fileName, CancellationToken ct);
    Task<MatchingServiceResponse> MatchAsync(MatchingServiceRequest request, CancellationToken ct);
    Task<RecommendationServiceResponse> RecommendAsync(RecommendationServiceRequest request, CancellationToken ct);
}

public interface ICvAnalysisService
{
    Task<CVAnalysisDto> AnalyzeMineAsync(int userId, int cvId, CancellationToken ct);
    Task<MatchResultDto> MatchMineAsync(int userId, int cvId, int jobId, CancellationToken ct);
    Task<IReadOnlyList<MatchResultDto>> GetMatchesMineAsync(int userId, int cvId, CancellationToken ct);
    Task<IReadOnlyList<RecommendedJobDto>> GetRecommendationsMineAsync(int userId, int cvId, int topK, CancellationToken ct);
}
