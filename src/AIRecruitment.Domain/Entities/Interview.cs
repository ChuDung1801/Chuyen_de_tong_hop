namespace AIRecruitment.Domain.Entities;

// ============== 
// TV4 — Lịch phỏng vấn thuộc về một hồ sơ ứng tuyển.
// ==============
public sealed class Interview
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime ScheduledAtUtc { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public string? MeetingUrl { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "Scheduled";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public JobApplication? Application { get; set; }
    public User? CreatedBy { get; set; }
}
