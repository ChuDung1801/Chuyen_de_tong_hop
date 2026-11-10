using System.Security.Claims;
using AIRecruitment.Application.DTOs;
using AIRecruitment.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruitment.Presentation.Controllers;

// ============== 
// TV2 — Người dùng chỉ đọc/cập nhật hồ sơ gắn với JWT của chính mình.
// ==============
[ApiController, Authorize, Route("api/profiles")]
public sealed class ProfilesController(IProfileService profiles) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        if (User.IsInRole("Candidate")) { var p = await profiles.GetCandidateAsync(UserId, ct); return p is null ? NotFound() : Ok(p); }
        if (User.IsInRole("Recruiter")) { var p = await profiles.GetRecruiterAsync(UserId, ct); return p is null ? NotFound() : Ok(p); }
        return Ok(new { userId = UserId, role = User.FindFirstValue(ClaimTypes.Role), message = "Tài khoản quản trị không có hồ sơ cá nhân trong Sprint 2." });
    }

    [HttpPut("me/candidate"), Authorize(Roles = "Candidate")]
    public async Task<IActionResult> UpdateCandidate(UpdateCandidateProfileRequest request, CancellationToken ct)
    { try { return Ok(await profiles.UpdateCandidateAsync(UserId, request, ct)); } catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); } catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); } }

    [HttpPut("me/recruiter"), Authorize(Roles = "Recruiter")]
    public async Task<IActionResult> UpdateRecruiter(UpdateRecruiterProfileRequest request, CancellationToken ct)
    { try { return Ok(await profiles.UpdateRecruiterAsync(UserId, request, ct)); } catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); } catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); } }
}
