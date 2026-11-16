namespace AIRecruitment.Domain.Entities;

// ============== 
// TV3 — Thông tin công ty do Recruiter quản lý.
// ==============
public sealed class Company
{
    public int Id { get; set; }
    public int OwnerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? Industry { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public User? Owner { get; set; }
    public ICollection<Job> Jobs { get; set; } = new List<Job>();
}
