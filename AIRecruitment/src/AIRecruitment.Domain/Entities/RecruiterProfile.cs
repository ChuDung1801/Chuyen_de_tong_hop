namespace AIRecruitment.Domain.Entities;

// ============== 
// TV3 — Hồ sơ nhà tuyển dụng.
// ==============
public sealed class RecruiterProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Position { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public User? User { get; set; }
}
