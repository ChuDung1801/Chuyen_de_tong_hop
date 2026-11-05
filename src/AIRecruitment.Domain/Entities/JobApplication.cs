namespace AIRecruitment.Domain.Entities;

// ============== 
// TV4 — Vòng đời hồ sơ ứng tuyển; trạng thái được kiểm tra tại ApplicationService.
// ==============
public sealed class JobApplication
{
    public int Id { get; set; }
    public int CandidateProfileId { get; set; }
    public int JobId { get; set; }
    public int? CVId { get; set; }
    public string? CoverLetter { get; set; }
    public string Status { get; set; } = ApplicationStatuses.Pending;
    public DateTime AppliedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public CandidateProfile? CandidateProfile { get; set; }
    public Job? Job { get; set; }
    public CV? CV { get; set; }
    public ICollection<Interview> Interviews { get; set; } = new List<Interview>();
}

public static class ApplicationStatuses
{
    public const string Pending = "Pending";
    public const string Reviewing = "Reviewing";
    public const string Shortlisted = "Shortlisted";
    public const string InterviewScheduled = "InterviewScheduled";
    public const string Rejected = "Rejected";
    public const string Hired = "Hired";
    public const string Withdrawn = "Withdrawn";
    public static readonly string[] All = [Pending, Reviewing, Shortlisted, InterviewScheduled, Rejected, Hired, Withdrawn];
}
