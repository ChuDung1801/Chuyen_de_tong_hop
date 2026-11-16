using AIRecruitment.Application.DTOs;
using AIRecruitment.Application.Interfaces;
using AIRecruitment.Domain.Entities;
using AIRecruitment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruitment.Infrastructure.Services;

// ============== 
// TV4 — Ứng tuyển, phân quyền theo Candidate/Recruiter và quản lý lịch phỏng vấn.
// ==============
public sealed class ApplicationService(ApplicationDbContext db) : IApplicationService
{
    public async Task<ApplicationDto> ApplyAsync(int userId, int jobId, ApplyJobRequest request, CancellationToken ct)
    {
        var profile = await db.CandidateProfiles.SingleOrDefaultAsync(x => x.UserId == userId, ct)
            ?? throw new InvalidOperationException("Bạn cần hoàn thiện hồ sơ ứng viên trước khi ứng tuyển.");
        var job = await db.Jobs.SingleOrDefaultAsync(x => x.Id == jobId && x.IsPublished, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy tin tuyển dụng đang mở.");
        if (request.CoverLetter?.Length > 4000) throw new ArgumentException("Thư ứng tuyển tối đa 4000 ký tự.");
        if (request.CVId is int cvId && !await db.CVs.AnyAsync(x => x.Id == cvId && x.CandidateProfileId == profile.Id, ct))
            throw new UnauthorizedAccessException("CV không thuộc hồ sơ của bạn.");
        if (await db.JobApplications.AnyAsync(x => x.CandidateProfileId == profile.Id && x.JobId == jobId, ct))
            throw new InvalidOperationException("Bạn đã ứng tuyển vị trí này rồi.");
        var application = new JobApplication { CandidateProfileId = profile.Id, JobId = job.Id, CVId = request.CVId, CoverLetter = request.CoverLetter?.Trim(), Status = ApplicationStatuses.Pending };
        db.JobApplications.Add(application);
        await db.SaveChangesAsync(ct);
        return await LoadDtoAsync(application.Id, ct) ?? throw new InvalidOperationException("Không thể tải hồ sơ ứng tuyển vừa tạo.");
    }

    public async Task<IReadOnlyList<ApplicationDto>> GetMineAsync(int userId, CancellationToken ct)
    {
        var profile = await db.CandidateProfiles.SingleOrDefaultAsync(x => x.UserId == userId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy hồ sơ ứng viên.");
        var ids = await db.JobApplications.Where(x => x.CandidateProfileId == profile.Id).OrderByDescending(x => x.AppliedAtUtc).Select(x => x.Id).ToListAsync(ct);
        var results = new List<ApplicationDto>();
        foreach (var id in ids) { var dto = await LoadDtoAsync(id, ct); if (dto is not null) results.Add(dto); }
        return results;
    }

    public async Task<IReadOnlyList<ApplicationDto>> GetForJobAsync(int userId, bool isAdmin, int jobId, CancellationToken ct)
    {
        var job = await db.Jobs.Include(x => x.Company).SingleOrDefaultAsync(x => x.Id == jobId, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy tin tuyển dụng.");
        EnsureJobOwner(job, userId, isAdmin);
        var ids = await db.JobApplications.Where(x => x.JobId == jobId).OrderByDescending(x => x.AppliedAtUtc).Select(x => x.Id).ToListAsync(ct);
        var results = new List<ApplicationDto>();
        foreach (var id in ids) { var dto = await LoadDtoAsync(id, ct); if (dto is not null) results.Add(dto); }
        return results;
    }

    public async Task<ApplicationDto?> UpdateStatusAsync(int userId, bool isAdmin, int applicationId, string status, CancellationToken ct)
    {
        status = status?.Trim() ?? "";
        if (!ApplicationStatuses.All.Contains(status, StringComparer.Ordinal)) throw new ArgumentException("Trạng thái ứng tuyển không hợp lệ.");
        var app = await db.JobApplications.Include(x => x.Job).ThenInclude(x => x!.Company).SingleOrDefaultAsync(x => x.Id == applicationId, ct);
        if (app is null) return null;
        EnsureJobOwner(app.Job!, userId, isAdmin);
        if (app.Status is ApplicationStatuses.Hired or ApplicationStatuses.Rejected or ApplicationStatuses.Withdrawn)
            throw new InvalidOperationException("Hồ sơ đã ở trạng thái kết thúc và không thể thay đổi.");
        app.Status = status; app.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await LoadDtoAsync(applicationId, ct);
    }

    public async Task<InterviewDto> CreateInterviewAsync(int userId, bool isAdmin, int applicationId, CreateInterviewRequest request, CancellationToken ct)
    {
        if (request.ScheduledAtUtc.Kind == DateTimeKind.Unspecified) throw new ArgumentException("ScheduledAtUtc phải có múi giờ hoặc được gửi dưới dạng UTC.");
        var scheduled = request.ScheduledAtUtc.ToUniversalTime();
        if (scheduled <= DateTime.UtcNow.AddMinutes(5)) throw new ArgumentException("Lịch phỏng vấn cần cách thời điểm hiện tại ít nhất 5 phút.");
        if (request.DurationMinutes is < 15 or > 240) throw new ArgumentException("Thời lượng phỏng vấn phải từ 15 đến 240 phút.");
        if (request.MeetingUrl is { Length: > 1000 } || request.Location is { Length: > 300 } || request.Notes is { Length: > 3000 }) throw new ArgumentException("Một trường lịch phỏng vấn vượt quá độ dài cho phép.");
        var app = await db.JobApplications.Include(x => x.Job).ThenInclude(x => x!.Company).SingleOrDefaultAsync(x => x.Id == applicationId, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy hồ sơ ứng tuyển.");
        EnsureJobOwner(app.Job!, userId, isAdmin);
        if (app.Status is ApplicationStatuses.Rejected or ApplicationStatuses.Withdrawn or ApplicationStatuses.Hired)
            throw new InvalidOperationException("Không thể đặt lịch cho hồ sơ đã kết thúc.");
        var interview = new Interview { ApplicationId = app.Id, CreatedByUserId = userId, ScheduledAtUtc = scheduled, DurationMinutes = request.DurationMinutes, MeetingUrl = request.MeetingUrl?.Trim(), Location = request.Location?.Trim(), Notes = request.Notes?.Trim() };
        db.Interviews.Add(interview); app.Status = ApplicationStatuses.InterviewScheduled; app.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await LoadInterviewDtoAsync(interview.Id, ct) ?? throw new InvalidOperationException("Không thể tải lịch phỏng vấn vừa tạo.");
    }

    public async Task<IReadOnlyList<InterviewDto>> GetMyInterviewsAsync(int userId, CancellationToken ct)
    {
        var role = await db.Users.Where(x => x.Id == userId).Select(x => x.Role).SingleOrDefaultAsync(ct);
        if (role is null) throw new KeyNotFoundException("Không tìm thấy tài khoản.");
        var idsQuery = db.Interviews.AsQueryable();
        if (role == UserRoles.Candidate)
        {
            var profileId = await db.CandidateProfiles.Where(x => x.UserId == userId).Select(x => (int?)x.Id).SingleOrDefaultAsync(ct);
            if (!profileId.HasValue) return Array.Empty<InterviewDto>();
            idsQuery = idsQuery.Where(x => x.Application!.CandidateProfileId == profileId.Value);
        }
        else if (role != UserRoles.Admin)
        {
            idsQuery = idsQuery.Where(x => x.Application!.Job!.CreatedByUserId == userId || x.Application!.Job!.Company!.OwnerUserId == userId);
        }
        var ids = await idsQuery.OrderBy(x => x.ScheduledAtUtc).Select(x => x.Id).ToListAsync(ct);
        var results = new List<InterviewDto>();
        foreach (var id in ids) { var dto = await LoadInterviewDtoAsync(id, ct); if (dto is not null) results.Add(dto); }
        return results;
    }

    private void EnsureJobOwner(Job job, int userId, bool isAdmin)
    {
        if (isAdmin) return;
        if (job.CreatedByUserId != userId && job.Company?.OwnerUserId != userId) throw new UnauthorizedAccessException("Bạn không có quyền truy cập hồ sơ ứng tuyển của tin này.");
    }

    private Task<ApplicationDto?> LoadDtoAsync(int id, CancellationToken ct) => db.JobApplications.AsNoTracking()
        .Where(x => x.Id == id)
        .Select(x => new ApplicationDto(x.Id, x.CandidateProfileId, x.JobId, x.Job!.Title, x.CandidateProfile!.FullName, x.CandidateProfile.User!.Email, x.CVId, x.CV != null ? x.CV.OriginalFileName : null, x.CoverLetter, x.Status, x.AppliedAtUtc, x.UpdatedAtUtc))
        .SingleOrDefaultAsync(ct);

    private Task<InterviewDto?> LoadInterviewDtoAsync(int id, CancellationToken ct) => db.Interviews.AsNoTracking()
        .Where(x => x.Id == id)
        .Select(x => new InterviewDto(x.Id, x.ApplicationId, x.ScheduledAtUtc, x.DurationMinutes, x.MeetingUrl, x.Location, x.Notes, x.Status, x.Application!.Job!.Title, x.Application.CandidateProfile!.FullName))
        .SingleOrDefaultAsync(ct);
}
