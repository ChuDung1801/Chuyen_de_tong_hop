namespace AIRecruitment.Application.DTOs;

// ============== 
// TV1 — DTOs không bao giờ trả PasswordHash cho client.
// ==============
public sealed record RegisterRequest(string Email, string Password, string Role, string FullName);
public sealed record LoginRequest(string Email, string Password);
public sealed record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, int UserId, string Email, string Role);
