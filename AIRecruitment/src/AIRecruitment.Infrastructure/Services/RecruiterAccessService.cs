using System.Text.RegularExpressions;
using AIRecruitment.Application.DTOs;
using AIRecruitment.Application.Interfaces;
using AIRecruitment.Domain.Entities;
using AIRecruitment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruitment.Infrastructure.Services;

public sealed class RecruiterAccessService(ApplicationDbContext db, IRecruiterVerificationStorage storage) : IRecruiterAccessService
{
    public async Task<RecruiterAccessRequestDto> SubmitAsync(int userId, SubmitRecruiterAccessRequest request,
        string fileName, string contentType, Stream content, long sizeBytes, CancellationToken cancellationToken)
    {
        var user = await db.Users.Include(x => x.CandidateProfile).Include(x => x.RecruiterProfile)
            .SingleOrDefaultAsync(x => x.Id == userId && x.IsActive, cancellationToken)
            ?? throw new UnauthorizedAccessException("Không tìm thấy tài khoản đang hoạt động.");

        if (user.Role == UserRoles.Admin || (user.Role == UserRoles.Recruiter && user.IsRecruiterVerified))
            throw new InvalidOperationException("Tài khoản đã có quyền phù hợp, không cần gửi yêu cầu xác minh.");
        if (user.Role is not UserRoles.Candidate and not UserRoles.Recruiter)
            throw new UnauthorizedAccessException("Tài khoản không được phép gửi yêu cầu này.");

        ValidateRequest(request);
        if (await db.RecruiterAccessRequests.AnyAsync(x => x.UserId == userId && x.Status == RecruiterAccessRequestStatuses.Pending, cancellationToken))
            throw new InvalidOperationException("Bạn đã có một yêu cầu đang chờ Admin xét duyệt.");

        var taxCode = NormalizeTaxCode(request.TaxCode);
        var taxCodeInUse = await db.RecruiterAccessRequests.AnyAsync(x => x.UserId != userId && x.TaxCode == taxCode
            && (x.Status == RecruiterAccessRequestStatuses.Pending || x.Status == RecruiterAccessRequestStatuses.Approved), cancellationToken);
        if (taxCodeInUse) throw new InvalidOperationException("Mã số thuế này đã được sử dụng trong một hồ sơ đang chờ hoặc đã được duyệt.");

        var stored = await storage.SaveAsync(fileName, contentType, content, sizeBytes, cancellationToken);
        var entity = new RecruiterAccessRequest
        {
            UserId = userId,
            ApplicantFullName = request.ApplicantFullName.Trim(),
            ApplicantPhone = request.ApplicantPhone.Trim(),
            ApplicantPosition = request.ApplicantPosition.Trim(),
            CompanyName = request.CompanyName.Trim(),
            TaxCode = taxCode,
            Website = NormalizeOptional(request.Website),
            Industry = NormalizeOptional(request.Industry),
            CompanyAddress = request.CompanyAddress.Trim(),
            CompanyDescription = NormalizeOptional(request.CompanyDescription),
            ApplicantNote = NormalizeOptional(request.ApplicantNote),
            DocumentOriginalFileName = Path.GetFileName(fileName),
            DocumentStoredFileName = stored.StoredFileName,
            DocumentContentType = stored.ContentType,
            DocumentSizeBytes = stored.SizeBytes,
            Status = RecruiterAccessRequestStatuses.Pending,
            SubmittedAtUtc = DateTime.UtcNow
        };

        try
        {
            db.RecruiterAccessRequests.Add(entity);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await storage.DeleteAsync(stored.StoredFileName, CancellationToken.None);
            throw;
        }
        return ToDto(entity);
    }

    public async Task<RecruiterAccessRequestDto?> GetMyLatestAsync(int userId, CancellationToken cancellationToken)
    {
        var entity = await db.RecruiterAccessRequests.AsNoTracking()
            .Where(x => x.UserId == userId).OrderByDescending(x => x.SubmittedAtUtc).FirstOrDefaultAsync(cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<IReadOnlyList<AdminRecruiterAccessRequestDto>> GetForAdminAsync(string? status, CancellationToken cancellationToken)
    {
        var query = db.RecruiterAccessRequests.AsNoTracking().Include(x => x.User).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalized = status.Trim();
            if (normalized is not RecruiterAccessRequestStatuses.Pending and not RecruiterAccessRequestStatuses.Approved and not RecruiterAccessRequestStatuses.Rejected)
                throw new ArgumentException("Trạng thái lọc không hợp lệ.");
            query = query.Where(x => x.Status == normalized);
        }
        return await query.OrderBy(x => x.Status == RecruiterAccessRequestStatuses.Pending ? 0 : 1)
            .ThenBy(x => x.SubmittedAtUtc)
            .Select(x => new AdminRecruiterAccessRequestDto(x.Id, x.UserId, x.User!.Email,
                x.ApplicantFullName, x.ApplicantPhone, x.ApplicantPosition, x.CompanyName, x.TaxCode,
                x.Website, x.Industry, x.CompanyAddress, x.CompanyDescription, x.ApplicantNote,
                x.DocumentOriginalFileName, x.DocumentContentType, x.DocumentSizeBytes, x.Status,
                x.SubmittedAtUtc, x.ReviewNote))
            .ToListAsync(cancellationToken);
    }

    public async Task<RecruiterDocumentDownload?> GetDocumentForAdminAsync(int requestId, CancellationToken cancellationToken)
    {
        var item = await db.RecruiterAccessRequests.AsNoTracking().SingleOrDefaultAsync(x => x.Id == requestId, cancellationToken);
        if (item is null) return null;
        var stream = await storage.OpenReadAsync(item.DocumentStoredFileName, cancellationToken);
        return stream is null ? null : new RecruiterDocumentDownload(stream, item.DocumentOriginalFileName, item.DocumentContentType);
    }

    public async Task<RecruiterAccessRequestDto?> ReviewAsync(int adminUserId, int requestId,
        ReviewRecruiterAccessRequest request, CancellationToken cancellationToken)
    {
        if (!request.Approve && string.IsNullOrWhiteSpace(request.Note))
            throw new ArgumentException("Khi từ chối, Admin phải ghi rõ lý do.");
        if ((request.Note?.Length ?? 0) > 2000) throw new ArgumentException("Ghi chú xét duyệt tối đa 2.000 ký tự.");

        var entity = await db.RecruiterAccessRequests.SingleOrDefaultAsync(x => x.Id == requestId, cancellationToken);
        if (entity is null) return null;
        if (entity.Status != RecruiterAccessRequestStatuses.Pending)
            throw new InvalidOperationException("Yêu cầu này đã được xét duyệt trước đó.");

        var user = await db.Users.Include(x => x.RecruiterProfile).Include(x => x.CandidateProfile)
            .SingleOrDefaultAsync(x => x.Id == entity.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Tài khoản gửi yêu cầu không còn tồn tại.");

        if (request.Approve)
        {
            var duplicateTaxCode = await db.RecruiterAccessRequests.AnyAsync(x => x.Id != entity.Id
                && x.TaxCode == entity.TaxCode && x.Status == RecruiterAccessRequestStatuses.Approved, cancellationToken);
            if (duplicateTaxCode) throw new InvalidOperationException("Một hồ sơ khác đã được duyệt với mã số thuế này.");

            user.Role = UserRoles.Recruiter;
            user.IsRecruiterVerified = true;
            if (user.RecruiterProfile is null)
            {
                user.RecruiterProfile = new RecruiterProfile
                {
                    FullName = entity.ApplicantFullName,
                    Phone = entity.ApplicantPhone,
                    Position = entity.ApplicantPosition,
                    UpdatedAtUtc = DateTime.UtcNow
                };
            }
            else
            {
                user.RecruiterProfile.FullName = entity.ApplicantFullName;
                user.RecruiterProfile.Phone = entity.ApplicantPhone;
                user.RecruiterProfile.Position = entity.ApplicantPosition;
                user.RecruiterProfile.UpdatedAtUtc = DateTime.UtcNow;
            }

            var existingCompany = await db.Companies.Where(x => x.OwnerUserId == user.Id).OrderBy(x => x.Id).FirstOrDefaultAsync(cancellationToken);
            if (existingCompany is null)
            {
                db.Companies.Add(new Company
                {
                    OwnerUserId = user.Id,
                    Name = entity.CompanyName,
                    Website = entity.Website,
                    Industry = entity.Industry,
                    Description = entity.CompanyDescription,
                    Location = entity.CompanyAddress,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
            else
            {
                existingCompany.Name = entity.CompanyName;
                existingCompany.Website = entity.Website;
                existingCompany.Industry = entity.Industry;
                existingCompany.Description = entity.CompanyDescription;
                existingCompany.Location = entity.CompanyAddress;
            }
            entity.Status = RecruiterAccessRequestStatuses.Approved;
        }
        else
        {
            entity.Status = RecruiterAccessRequestStatuses.Rejected;
        }

        entity.ReviewedByUserId = adminUserId;
        entity.ReviewedAtUtc = DateTime.UtcNow;
        entity.ReviewNote = NormalizeOptional(request.Note);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    private static void ValidateRequest(SubmitRecruiterAccessRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ApplicantFullName) || request.ApplicantFullName.Trim().Length > 150)
            throw new ArgumentException("Họ tên người đại diện là bắt buộc và tối đa 150 ký tự.");
        if (string.IsNullOrWhiteSpace(request.ApplicantPhone) || request.ApplicantPhone.Trim().Length > 40)
            throw new ArgumentException("Số điện thoại liên hệ là bắt buộc.");
        if (string.IsNullOrWhiteSpace(request.ApplicantPosition) || request.ApplicantPosition.Trim().Length > 150)
            throw new ArgumentException("Chức vụ người đăng ký là bắt buộc.");
        if (string.IsNullOrWhiteSpace(request.CompanyName) || request.CompanyName.Trim().Length > 200)
            throw new ArgumentException("Tên công ty là bắt buộc và tối đa 200 ký tự.");
        if (string.IsNullOrWhiteSpace(request.TaxCode) || NormalizeTaxCode(request.TaxCode).Length is < 8 or > 20)
            throw new ArgumentException("Mã số thuế không hợp lệ (nhập từ 8 đến 20 ký tự chữ/số).");
        if (string.IsNullOrWhiteSpace(request.CompanyAddress) || request.CompanyAddress.Trim().Length > 300)
            throw new ArgumentException("Địa chỉ công ty là bắt buộc.");
        if (!string.IsNullOrWhiteSpace(request.Website)
            && (!Uri.TryCreate(request.Website.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
            throw new ArgumentException("Website phải là địa chỉ http hoặc https hợp lệ.");
        if ((request.CompanyDescription?.Length ?? 0) > 3000 || (request.ApplicantNote?.Length ?? 0) > 2000)
            throw new ArgumentException("Mô tả công ty tối đa 3.000 ký tự; ghi chú tối đa 2.000 ký tự.");
    }

    private static string NormalizeTaxCode(string value) => Regex.Replace(value, "[^a-zA-Z0-9]", "").ToUpperInvariant();
    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static RecruiterAccessRequestDto ToDto(RecruiterAccessRequest x) => new(
        x.Id, x.Status, x.ApplicantFullName, x.ApplicantPhone, x.ApplicantPosition, x.CompanyName,
        x.TaxCode, x.Website, x.Industry, x.CompanyAddress, x.CompanyDescription, x.ApplicantNote,
        x.DocumentOriginalFileName, x.DocumentSizeBytes, x.SubmittedAtUtc, x.ReviewedAtUtc, x.ReviewNote);
}
