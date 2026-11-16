namespace AIRecruitment.Application.DTOs;

public sealed record SubmitRecruiterAccessRequest(
    string ApplicantFullName, string ApplicantPhone, string ApplicantPosition,
    string CompanyName, string TaxCode, string? Website, string? Industry,
    string CompanyAddress, string? CompanyDescription, string? ApplicantNote);

public sealed record RecruiterAccessRequestDto(
    int Id, string Status, string ApplicantFullName, string ApplicantPhone, string ApplicantPosition,
    string CompanyName, string TaxCode, string? Website, string? Industry, string CompanyAddress,
    string? CompanyDescription, string? ApplicantNote, string DocumentOriginalFileName, long DocumentSizeBytes,
    DateTime SubmittedAtUtc, DateTime? ReviewedAtUtc, string? ReviewNote);

public sealed record AdminRecruiterAccessRequestDto(
    int Id, int UserId, string Email, string ApplicantFullName, string ApplicantPhone, string ApplicantPosition,
    string CompanyName, string TaxCode, string? Website, string? Industry, string CompanyAddress,
    string? CompanyDescription, string? ApplicantNote, string DocumentOriginalFileName, string DocumentContentType,
    long DocumentSizeBytes, string Status, DateTime SubmittedAtUtc, string? ReviewNote);

public sealed record ReviewRecruiterAccessRequest(bool Approve, string? Note);
public sealed record RecruiterDocumentDownload(Stream Content, string FileName, string ContentType);
public sealed record StoredRecruiterDocument(string StoredFileName, string ContentType, long SizeBytes);
