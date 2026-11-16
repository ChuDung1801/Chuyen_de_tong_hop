namespace AIRecruitment.Domain.Entities;

// ============== 
// TV2 — Metadata CV. Không lưu nội dung file trực tiếp trong SQL Server.
// ==============
public sealed class CV
{
    public int Id { get; set; }
    public int CandidateProfileId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
    public CandidateProfile? CandidateProfile { get; set; }
}
