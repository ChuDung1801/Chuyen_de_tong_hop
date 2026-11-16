namespace AIRecruitment.Domain.Entities;

public static class RecruiterAccessRequestStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}

// Hồ sơ xin cấp quyền nhà tuyển dụng. File chứng minh được lưu ngoài web root.
public sealed class RecruiterAccessRequest
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string ApplicantFullName { get; set; } = string.Empty;
    public string ApplicantPhone { get; set; } = string.Empty;
    public string ApplicantPosition { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string TaxCode { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? Industry { get; set; }
    public string CompanyAddress { get; set; } = string.Empty;
    public string? CompanyDescription { get; set; }
    public string? ApplicantNote { get; set; }
    public string DocumentOriginalFileName { get; set; } = string.Empty;
    public string DocumentStoredFileName { get; set; } = string.Empty;
    public string DocumentContentType { get; set; } = string.Empty;
    public long DocumentSizeBytes { get; set; }
    public string Status { get; set; } = RecruiterAccessRequestStatuses.Pending;
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewNote { get; set; }
    public User? User { get; set; }
}
