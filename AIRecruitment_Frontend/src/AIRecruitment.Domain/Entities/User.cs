namespace AIRecruitment.Domain.Entities;

// ============== 
// TV1 — Authentication: tài khoản, hash mật khẩu và vai trò.
// ==============
public sealed class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = UserRoles.Candidate;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public CandidateProfile? CandidateProfile { get; set; }
    public RecruiterProfile? RecruiterProfile { get; set; }
}

public static class UserRoles
{
    public const string Candidate = "Candidate";
    public const string Recruiter = "Recruiter";
    public const string Admin = "Admin";
}
