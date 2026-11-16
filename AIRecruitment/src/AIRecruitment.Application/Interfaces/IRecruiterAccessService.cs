using AIRecruitment.Application.DTOs;

namespace AIRecruitment.Application.Interfaces;

public interface IRecruiterVerificationStorage
{
    Task<StoredRecruiterDocument> SaveAsync(string originalFileName, string contentType, Stream content, long sizeBytes, CancellationToken cancellationToken);
    Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken);
    Task DeleteAsync(string storedFileName, CancellationToken cancellationToken);
}

public interface IRecruiterAccessService
{
    Task<RecruiterAccessRequestDto> SubmitAsync(int userId, SubmitRecruiterAccessRequest request, string fileName, string contentType, Stream content, long sizeBytes, CancellationToken cancellationToken);
    Task<RecruiterAccessRequestDto?> GetMyLatestAsync(int userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AdminRecruiterAccessRequestDto>> GetForAdminAsync(string? status, CancellationToken cancellationToken);
    Task<RecruiterDocumentDownload?> GetDocumentForAdminAsync(int requestId, CancellationToken cancellationToken);
    Task<RecruiterAccessRequestDto?> ReviewAsync(int adminUserId, int requestId, ReviewRecruiterAccessRequest request, CancellationToken cancellationToken);
}
