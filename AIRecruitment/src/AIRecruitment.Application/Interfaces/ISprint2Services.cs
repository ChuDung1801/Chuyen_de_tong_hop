using AIRecruitment.Application.DTOs;

namespace AIRecruitment.Application.Interfaces;

// ============== 
// TV1 — Hợp đồng xác thực.
// ==============
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}

// ============== 
// TV2 — Hồ sơ ứng viên và upload CV.
// ==============
public interface IProfileService
{
    Task<CandidateProfileDto?> GetCandidateAsync(int userId, CancellationToken cancellationToken);
    Task<CandidateProfileDto> UpdateCandidateAsync(int userId, UpdateCandidateProfileRequest request, CancellationToken cancellationToken);
    Task<RecruiterProfileDto?> GetRecruiterAsync(int userId, CancellationToken cancellationToken);
    Task<RecruiterProfileDto> UpdateRecruiterAsync(int userId, UpdateRecruiterProfileRequest request, CancellationToken cancellationToken);
    Task<CvDto> UploadCvAsync(int userId, string originalFileName, string contentType, Stream content, long sizeBytes, CancellationToken cancellationToken);
    Task<IReadOnlyList<CvDto>> GetMyCvsAsync(int userId, CancellationToken cancellationToken);
}

public interface ICvStorage
{
    Task<(string storedName, string fullPath)> SaveAsync(string originalFileName, Stream content, CancellationToken cancellationToken);
}

// ============== 
// TV3 — Company/Job.
// ==============
public interface ICompanyJobService
{
    Task<IReadOnlyList<CompanyDto>> GetMyCompaniesAsync(int userId, CancellationToken cancellationToken);
    Task<CompanyDto> CreateCompanyAsync(int userId, CreateCompanyRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<JobResponseDto>> GetJobsAsync(string? search, CancellationToken cancellationToken);
    Task<JobResponseDto?> GetJobAsync(int id, CancellationToken cancellationToken);
    Task<JobResponseDto> CreateJobAsync(int userId, CreateRecruitmentJobRequest request, CancellationToken cancellationToken);
}
