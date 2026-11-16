namespace AIRecruitment.Domain.Entities;

// ============== 
// TV2 — Danh sách việc làm đã lưu của ứng viên.
// ==============
public sealed class SavedJob
{
    public int Id { get; set; }
    public int CandidateProfileId { get; set; }
    public int JobId { get; set; }
    public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;
    public CandidateProfile? CandidateProfile { get; set; }
    public Job? Job { get; set; }
}
