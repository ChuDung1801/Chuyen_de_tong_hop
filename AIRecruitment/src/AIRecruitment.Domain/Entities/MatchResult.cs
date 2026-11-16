namespace AIRecruitment.Domain.Entities;

// ============== 
// TV5 — Kết quả matching truy vết được theo CV, Job và phiên bản mô hình.
// ==============
public sealed class MatchResult
{
    public long Id { get; set; }
    public int CVId { get; set; }
    public int JobId { get; set; }
    public int? AIModelVersionId { get; set; }
    public decimal MatchScore { get; set; }
    public string MatchingSkillsJson { get; set; } = "[]";
    public string MissingSkillsJson { get; set; } = "[]";
    public string? Explanation { get; set; }
    public string FeatureValuesJson { get; set; } = "{}";
    public string ModelVersion { get; set; } = "demo-v0";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public CV? CV { get; set; }
    public Job? Job { get; set; }
    public AIModelVersion? AIModelVersion { get; set; }
}
