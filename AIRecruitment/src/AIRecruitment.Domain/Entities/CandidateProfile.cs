namespace AIRecruitment.Domain.Entities;

// ============== 
// TV2 — Hồ sơ ứng viên và thông tin nghề nghiệp cơ bản.
// ==============
public sealed class CandidateProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public string? Summary { get; set; }
    public int? YearsExperience { get; set; }
    public string? SkillsCsv { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public User? User { get; set; }
    public ICollection<CV> CVs { get; set; } = new List<CV>();
}
