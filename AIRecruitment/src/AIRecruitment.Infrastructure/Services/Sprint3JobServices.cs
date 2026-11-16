using AIRecruitment.Application.DTOs;
using AIRecruitment.Application.Interfaces;
using AIRecruitment.Domain.Entities;
using AIRecruitment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace AIRecruitment.Infrastructure.Services;

// ============== 
// TV3 — Search, filters, pagination, CRUD and recruiter ownership checks.
// ==============
public sealed class Sprint3JobService(ApplicationDbContext db) : ISprint3JobService
{
    private static JobResponseDto Map(Job j) => new(j.Id, j.CompanyId, j.Title, j.Description, j.Location, j.EmploymentType, j.RequiredSkillsCsv, j.CreatedAtUtc, j.IsPublished);

    public async Task<JobSearchPageDto> SearchAsync(string? search, string? location, string? employmentType, string? skill, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 50);
        var q = db.Jobs.AsNoTracking().Where(x => x.IsPublished);
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim(); q = q.Where(x => x.Title.Contains(term) || x.Description.Contains(term) || (x.RequiredSkillsCsv != null && x.RequiredSkillsCsv.Contains(term))); }
        if (!string.IsNullOrWhiteSpace(location)) { var term = location.Trim(); q = q.Where(x => x.Location.Contains(term)); }
        if (!string.IsNullOrWhiteSpace(employmentType)) q = q.Where(x => x.EmploymentType == employmentType.Trim());
        if (!string.IsNullOrWhiteSpace(skill)) { var term = skill.Trim(); q = q.Where(x => x.RequiredSkillsCsv != null && x.RequiredSkillsCsv.Contains(term)); }
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id).Skip((page-1)*pageSize).Take(pageSize).Select(x => new JobResponseDto(x.Id, x.CompanyId, x.Title, x.Description, x.Location, x.EmploymentType, x.RequiredSkillsCsv, x.CreatedAtUtc, x.IsPublished)).ToListAsync(ct);
        return new JobSearchPageDto(items, page, pageSize, total, (int)Math.Ceiling(total/(double)pageSize));
    }
    public async Task<JobResponseDto?> GetPublicAsync(int id, CancellationToken ct) => await db.Jobs.AsNoTracking().Where(x=>x.Id==id && x.IsPublished).Select(x=>new JobResponseDto(x.Id,x.CompanyId,x.Title,x.Description,x.Location,x.EmploymentType,x.RequiredSkillsCsv,x.CreatedAtUtc,x.IsPublished)).SingleOrDefaultAsync(ct);
    public async Task<JobResponseDto> CreateAsync(int userId, CreateRecruitmentJobRequest r, CancellationToken ct)
    {
        Validate(r.Title,r.Description,r.Location,r.EmploymentType,r.RequiredSkillsCsv);
        if (r.CompanyId.HasValue && !await db.Companies.AnyAsync(x=>x.Id==r.CompanyId && x.OwnerUserId==userId,ct)) throw new UnauthorizedAccessException("Bạn không sở hữu công ty đã chọn.");
        var j=new Job{CreatedByUserId=userId,CompanyId=r.CompanyId,Title=r.Title.Trim(),Description=r.Description.Trim(),Location=r.Location?.Trim()??"",EmploymentType=NormalizeType(r.EmploymentType),RequiredSkillsCsv=NormalizeCsv(r.RequiredSkillsCsv),IsPublished=r.IsPublished,CreatedAtUtc=DateTime.UtcNow};
        db.Jobs.Add(j); await db.SaveChangesAsync(ct); return Map(j);
    }
    public async Task<JobResponseDto?> UpdateAsync(int userId,bool isAdmin,int id,UpdateRecruitmentJobRequest r,CancellationToken ct)
    {
        Validate(r.Title,r.Description,r.Location,r.EmploymentType,r.RequiredSkillsCsv);
        var j=await db.Jobs.SingleOrDefaultAsync(x=>x.Id==id,ct); if(j is null) return null;
        if(!isAdmin && j.CreatedByUserId!=userId) throw new UnauthorizedAccessException("Bạn không có quyền sửa tin này.");
        if(r.CompanyId.HasValue && !await db.Companies.AnyAsync(x=>x.Id==r.CompanyId && (isAdmin || x.OwnerUserId==userId),ct)) throw new UnauthorizedAccessException("Bạn không sở hữu công ty đã chọn.");
        j.CompanyId=r.CompanyId;j.Title=r.Title.Trim();j.Description=r.Description.Trim();j.Location=r.Location?.Trim()??"";j.EmploymentType=NormalizeType(r.EmploymentType);j.RequiredSkillsCsv=NormalizeCsv(r.RequiredSkillsCsv);j.IsPublished=r.IsPublished;
        await db.SaveChangesAsync(ct); return Map(j);
    }
    public async Task<bool> DeleteAsync(int userId,bool isAdmin,int id,CancellationToken ct)
    { var j=await db.Jobs.SingleOrDefaultAsync(x=>x.Id==id,ct); if(j is null)return false; if(!isAdmin && j.CreatedByUserId!=userId)throw new UnauthorizedAccessException("Bạn không có quyền xóa tin này."); db.Jobs.Remove(j);await db.SaveChangesAsync(ct);return true; }
    public async Task<IReadOnlyList<JobResponseDto>> GetMineAsync(int userId,bool isAdmin,CancellationToken ct)
    { var q=db.Jobs.AsNoTracking().AsQueryable(); if(!isAdmin)q=q.Where(x=>x.CreatedByUserId==userId); return await q.OrderByDescending(x=>x.CreatedAtUtc).Select(x=>new JobResponseDto(x.Id,x.CompanyId,x.Title,x.Description,x.Location,x.EmploymentType,x.RequiredSkillsCsv,x.CreatedAtUtc,x.IsPublished)).ToListAsync(ct); }
    private static string NormalizeType(string? value) { var allowed=new[]{"FullTime","PartTime","Contract","Internship","Remote"}; var result=string.IsNullOrWhiteSpace(value)?"FullTime":value.Trim(); if(!allowed.Contains(result,StringComparer.OrdinalIgnoreCase))throw new ArgumentException("EmploymentType không hợp lệ."); return allowed.First(x=>x.Equals(result,StringComparison.OrdinalIgnoreCase)); }
    private static string? NormalizeCsv(string? csv) => string.IsNullOrWhiteSpace(csv)?null:string.Join(", ",csv.Split(',',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).Where(x=>x.Length>0).Distinct(StringComparer.OrdinalIgnoreCase));
    private static void Validate(string? title,string? description,string? location,string? type,string? skills)
    { if(string.IsNullOrWhiteSpace(title)||title.Trim().Length<2||title.Length>200)throw new ArgumentException("Tiêu đề phải có 2–200 ký tự."); if(string.IsNullOrWhiteSpace(description)||description.Trim().Length<10||description.Length>8000)throw new ArgumentException("Mô tả phải có 10–8000 ký tự."); if((location?.Length??0)>200||(skills?.Length??0)>2000)throw new ArgumentException("Địa điểm hoặc kỹ năng vượt quá giới hạn."); _=NormalizeType(type); }
}

// ============== 
// TV2 — Candidate-only saved jobs. Ownership is derived from authenticated user id.
// ==============
public sealed class SavedJobService(ApplicationDbContext db) : ISavedJobService
{
 public async Task<IReadOnlyList<SavedJobDto>> GetMineAsync(int userId,CancellationToken ct) => await db.SavedJobs.AsNoTracking().Where(x=>x.CandidateProfile!.UserId==userId).OrderByDescending(x=>x.SavedAtUtc).Select(x=>new SavedJobDto(x.JobId,x.Job!.Title,x.Job.Location,x.Job.EmploymentType,x.SavedAtUtc)).ToListAsync(ct);
 public async Task<bool> SaveAsync(int userId,int jobId,CancellationToken ct)
 { var profile=await db.CandidateProfiles.SingleOrDefaultAsync(x=>x.UserId==userId,ct)??throw new UnauthorizedAccessException("Chỉ ứng viên mới có thể lưu việc làm."); if(!await db.Jobs.AnyAsync(x=>x.Id==jobId&&x.IsPublished,ct))return false; if(await db.SavedJobs.AnyAsync(x=>x.CandidateProfileId==profile.Id&&x.JobId==jobId,ct))return true; db.SavedJobs.Add(new SavedJob{CandidateProfileId=profile.Id,JobId=jobId});await db.SaveChangesAsync(ct);return true; }
 public async Task<bool> RemoveAsync(int userId,int jobId,CancellationToken ct)
 { var item=await db.SavedJobs.SingleOrDefaultAsync(x=>x.JobId==jobId&&x.CandidateProfile!.UserId==userId,ct); if(item is null)return false;db.SavedJobs.Remove(item);await db.SaveChangesAsync(ct);return true; }
}
