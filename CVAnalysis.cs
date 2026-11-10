namespace AIRecruitment.Domain.Entities;

// ============== 
// TV5 — Kết quả phân tích CV; lưu dữ liệu có cấu trúc, không lưu toàn bộ văn bản CV.
// ==============
public sealed class CVAnalysis
{
    public long Id { get; set; }
    public int CVId { get; set; }
    public string ParserVersion { get; set; } = "initial";
    public string ExtractedSkillsJson { get; set; } = "[]";
    public int? YearsExperience { get; set; }
    public string? Education { get; set; }
    public string Status { get; set; } = "Pending";
    public string? ErrorCode { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public CV? CV { get; set; }
}
