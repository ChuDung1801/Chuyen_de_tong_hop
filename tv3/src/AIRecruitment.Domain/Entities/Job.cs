namespace AIRecruitment.Domain.Entities;

// ============== 
// TV3 — Job CRUD cơ bản; CompanyId và CreatedByUserId ràng buộc quyền sở hữu.
// ==============
public sealed class Job
{
    public int Id { get; set; }
    public int? CompanyId { get; set; }
    public int? CreatedByUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = "FullTime";
    public string? RequiredSkillsCsv { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsPublished { get; set; }
    public Company? Company { get; set; }
    public User? CreatedBy { get; set; }
}
