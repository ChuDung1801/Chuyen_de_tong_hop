namespace AIRecruitment.Domain.Entities;

// ============== 
// TV5 — Lưu phiên bản mô hình và metadata phục vụ truy vết kết quả AI.
// ==============
public sealed class AIModelVersion
{
    public int Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Algorithm { get; set; } = "unknown";
    public string DatasetDescription { get; set; } = "";
    public string MetricsJson { get; set; } = "{}";
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
