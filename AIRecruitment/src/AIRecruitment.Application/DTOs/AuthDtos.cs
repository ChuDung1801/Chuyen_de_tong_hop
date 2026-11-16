namespace AIRecruitment.Application.DTOs;

// Role trong request được giữ tương thích API cũ nhưng backend luôn tạo Candidate.
public sealed record RegisterRequest(string Email, string Password, string Role, string FullName);
public sealed record LoginRequest(string Email, string Password);
public sealed record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, int UserId, string Email, string Role, bool IsRecruiterVerified);
