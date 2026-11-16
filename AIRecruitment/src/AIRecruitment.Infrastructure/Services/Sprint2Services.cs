using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AIRecruitment.Application.DTOs;
using AIRecruitment.Application.Interfaces;
using AIRecruitment.Domain.Entities;
using AIRecruitment.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AIRecruitment.Infrastructure.Services;

// ============== 
// TV1 — Đăng ký/đăng nhập, hash password bằng PasswordHasher, phát hành JWT.
// ==============
public sealed class AuthService(ApplicationDbContext db, IConfiguration config) : IAuthService
{
    private readonly PasswordHasher<User> _hasher = new();

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) throw new ArgumentException("Email không hợp lệ.");
        if (request.Password.Length < 10 || !request.Password.Any(char.IsLetter) || !request.Password.Any(char.IsDigit))
            throw new ArgumentException("Mật khẩu phải có ít nhất 10 ký tự và gồm chữ, số.");
        if (await db.Users.AnyAsync(x => x.Email == email, cancellationToken)) throw new InvalidOperationException("Email đã được đăng ký.");
        // Không tin Role do client gửi lên. Mọi đăng ký công khai chỉ tạo Candidate.
        // Quyền Recruiter chỉ được cấp qua quy trình xác minh do Admin phê duyệt.
        var user = new User { Email = email, Role = UserRoles.Candidate, IsActive = true, IsRecruiterVerified = false };
        user.PasswordHash = _hasher.HashPassword(user, request.Password);
        user.CandidateProfile = new CandidateProfile { FullName = request.FullName.Trim() };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return CreateToken(user);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email && x.IsActive, cancellationToken);
        if (user is null || _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed) return null;
        return CreateToken(user);
    }

    private AuthResponse CreateToken(User user)
    {
        var secret = config["Jwt:Secret"] ?? throw new InvalidOperationException("Thiếu cấu hình Jwt:Secret.");
        if (Encoding.UTF8.GetByteCount(secret) < 32) throw new InvalidOperationException("Jwt:Secret phải dài tối thiểu 32 byte.");
        var expires = DateTime.UtcNow.AddHours(4);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("recruiter_verified", user.IsRecruiterVerified ? "true" : "false")
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var token = new JwtSecurityToken(claims: claims, expires: expires, signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), expires, user.Id, user.Email, user.Role, user.IsRecruiterVerified);
    }
}

// ============== 
// TV2 — Candidate/Recruiter profile và quản lý metadata CV.
// ==============
public sealed class ProfileService(ApplicationDbContext db, ICvStorage storage) : IProfileService
{
    public async Task<CandidateProfileDto?> GetCandidateAsync(int userId, CancellationToken ct) =>
        await db.CandidateProfiles.Include(x => x.User).Where(x => x.UserId == userId)
            .Select(x => new CandidateProfileDto(x.Id, x.User!.Email, x.FullName, x.Phone, x.Location, x.Summary, x.YearsExperience, x.SkillsCsv)).SingleOrDefaultAsync(ct);

    public async Task<CandidateProfileDto> UpdateCandidateAsync(int userId, UpdateCandidateProfileRequest request, CancellationToken ct)
    {
        var p = await db.CandidateProfiles.Include(x => x.User).SingleOrDefaultAsync(x => x.UserId == userId, ct) ?? throw new KeyNotFoundException("Không tìm thấy hồ sơ ứng viên.");
        if (string.IsNullOrWhiteSpace(request.FullName)) throw new ArgumentException("Họ tên không được để trống.");
        p.FullName = request.FullName.Trim(); p.Phone = request.Phone?.Trim(); p.Location = request.Location?.Trim(); p.Summary = request.Summary?.Trim();
        p.YearsExperience = request.YearsExperience is >= 0 and <= 70 ? request.YearsExperience : null; p.SkillsCsv = request.SkillsCsv?.Trim(); p.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new CandidateProfileDto(p.Id, p.User!.Email, p.FullName, p.Phone, p.Location, p.Summary, p.YearsExperience, p.SkillsCsv);
    }

    public async Task<RecruiterProfileDto?> GetRecruiterAsync(int userId, CancellationToken ct) =>
        await db.RecruiterProfiles.Include(x => x.User).Where(x => x.UserId == userId)
            .Select(x => new RecruiterProfileDto(x.Id, x.User!.Email, x.FullName, x.Phone, x.Position)).SingleOrDefaultAsync(ct);

    public async Task<RecruiterProfileDto> UpdateRecruiterAsync(int userId, UpdateRecruiterProfileRequest request, CancellationToken ct)
    {
        var p = await db.RecruiterProfiles.Include(x => x.User).SingleOrDefaultAsync(x => x.UserId == userId, ct) ?? throw new KeyNotFoundException("Không tìm thấy hồ sơ nhà tuyển dụng.");
        if (string.IsNullOrWhiteSpace(request.FullName)) throw new ArgumentException("Họ tên không được để trống.");
        p.FullName = request.FullName.Trim(); p.Phone = request.Phone?.Trim(); p.Position = request.Position?.Trim(); p.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new RecruiterProfileDto(p.Id, p.User!.Email, p.FullName, p.Phone, p.Position);
    }

    public async Task<CvDto> UploadCvAsync(int userId, string originalFileName, string contentType, Stream content, long sizeBytes, CancellationToken ct)
    {
        var profile = await db.CandidateProfiles.SingleOrDefaultAsync(x => x.UserId == userId, ct) ?? throw new UnauthorizedAccessException("Chỉ ứng viên mới được tải CV.");
        if (sizeBytes <= 0 || sizeBytes > 5 * 1024 * 1024) throw new ArgumentException("CV phải có dung lượng từ 1 byte đến 5 MB.");
        var ext = Path.GetExtension(Path.GetFileName(originalFileName)).ToLowerInvariant();
        if (ext is not ".pdf" and not ".doc" and not ".docx") throw new ArgumentException("Chỉ chấp nhận tệp PDF, DOC hoặc DOCX.");
        var (stored, fullPath) = await storage.SaveAsync(originalFileName, content, ct);
        var cv = new CV { CandidateProfileId = profile.Id, OriginalFileName = Path.GetFileName(originalFileName), StoredFileName = stored, StoragePath = fullPath, ContentType = contentType, SizeBytes = sizeBytes };
        db.CVs.Add(cv); await db.SaveChangesAsync(ct);
        return new CvDto(cv.Id, cv.OriginalFileName, cv.ContentType, cv.SizeBytes, cv.UploadedAtUtc);
    }

    public async Task<IReadOnlyList<CvDto>> GetMyCvsAsync(int userId, CancellationToken ct) =>
        await db.CVs.Where(x => x.CandidateProfile!.UserId == userId).OrderByDescending(x => x.UploadedAtUtc)
            .Select(x => new CvDto(x.Id, x.OriginalFileName, x.ContentType, x.SizeBytes, x.UploadedAtUtc)).ToListAsync(ct);
}

// ============== 
// TV3 — Company ownership and basic job listing/creation.
// ==============
public sealed class CompanyJobService(ApplicationDbContext db) : ICompanyJobService
{
    public async Task<IReadOnlyList<CompanyDto>> GetMyCompaniesAsync(int userId, CancellationToken ct) =>
        await db.Companies.Where(x => x.OwnerUserId == userId).OrderBy(x => x.Name)
            .Select(x => new CompanyDto(x.Id, x.Name, x.Website, x.Industry, x.Description, x.Location)).ToListAsync(ct);

    public async Task<CompanyDto> CreateCompanyAsync(int userId, CreateCompanyRequest r, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.Name)) throw new ArgumentException("Tên công ty không được để trống.");
        var c = new Company { OwnerUserId = userId, Name = r.Name.Trim(), Website = r.Website?.Trim(), Industry = r.Industry?.Trim(), Description = r.Description?.Trim(), Location = r.Location?.Trim() };
        db.Companies.Add(c); await db.SaveChangesAsync(ct);
        return new CompanyDto(c.Id, c.Name, c.Website, c.Industry, c.Description, c.Location);
    }

    public async Task<IReadOnlyList<JobResponseDto>> GetJobsAsync(string? search, CancellationToken ct)
    {
        var q = db.Jobs.AsNoTracking().Where(x => x.IsPublished);
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim(); q = q.Where(x => x.Title.Contains(term) || x.Description.Contains(term) || x.Location.Contains(term)); }
        return await q.OrderByDescending(x => x.CreatedAtUtc).Take(100).Select(x => new JobResponseDto(x.Id, x.CompanyId, x.Title, x.Description, x.Location, x.EmploymentType, x.RequiredSkillsCsv, x.CreatedAtUtc, x.IsPublished)).ToListAsync(ct);
    }

    public async Task<JobResponseDto?> GetJobAsync(int id, CancellationToken ct) =>
        await db.Jobs.AsNoTracking().Where(x => x.Id == id && x.IsPublished)
            .Select(x => new JobResponseDto(x.Id, x.CompanyId, x.Title, x.Description, x.Location, x.EmploymentType, x.RequiredSkillsCsv, x.CreatedAtUtc, x.IsPublished)).SingleOrDefaultAsync(ct);

    public async Task<JobResponseDto> CreateJobAsync(int userId, CreateRecruitmentJobRequest r, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.Title) || string.IsNullOrWhiteSpace(r.Description)) throw new ArgumentException("Tiêu đề và mô tả công việc là bắt buộc.");
        if (r.CompanyId.HasValue && !await db.Companies.AnyAsync(x => x.Id == r.CompanyId && x.OwnerUserId == userId, ct)) throw new UnauthorizedAccessException("Bạn không sở hữu công ty đã chọn.");
        var j = new Job { CreatedByUserId = userId, CompanyId = r.CompanyId, Title = r.Title.Trim(), Description = r.Description.Trim(), Location = r.Location?.Trim() ?? "", EmploymentType = string.IsNullOrWhiteSpace(r.EmploymentType) ? "FullTime" : r.EmploymentType.Trim(), RequiredSkillsCsv = r.RequiredSkillsCsv?.Trim(), IsPublished = r.IsPublished };
        db.Jobs.Add(j); await db.SaveChangesAsync(ct);
        return new JobResponseDto(j.Id, j.CompanyId, j.Title, j.Description, j.Location, j.EmploymentType, j.RequiredSkillsCsv, j.CreatedAtUtc, j.IsPublished);
    }
}
