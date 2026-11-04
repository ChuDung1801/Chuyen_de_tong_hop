using AIRecruitment.Application.DTOs;
using AIRecruitment.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruitment.Presentation.Controllers;

// ============== 
// TV1 — Endpoints đăng ký/đăng nhập công khai.
// ==============
[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        try { return Ok(await auth.RegisterAsync(request, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await auth.LoginAsync(request, ct);
        return result is null ? Unauthorized(new { message = "Email hoặc mật khẩu không đúng." }) : Ok(result);
    }
}
