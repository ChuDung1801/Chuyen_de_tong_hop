using System.Security.Claims;
using AIRecruitment.Application.DTOs;
using AIRecruitment.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruitment.Presentation.Controllers;

// ============== 
// TV4 — Candidate nộp hồ sơ; Recruiter/Admin xem hồ sơ và cập nhật trạng thái.
// ==============
[ApiController, Authorize, Route("api/applications")]
public sealed class ApplicationsController(IApplicationService service) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin => User.IsInRole("Admin");

    [HttpPost("~/api/jobs/{jobId:int}/applications"), Authorize(Roles = "Candidate")]
    public async Task<IActionResult> Apply(int jobId, ApplyJobRequest request, CancellationToken ct)
    {
        try { var result = await service.ApplyAsync(UserId, jobId, request, ct); return StatusCode(StatusCodes.Status201Created, result); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return ForbidWithMessage(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpGet("my"), Authorize(Roles = "Candidate")]
    public async Task<IActionResult> Mine(CancellationToken ct)
    { try { return Ok(await service.GetMineAsync(UserId, ct)); } catch (InvalidOperationException ex) { return NotFound(new { message = ex.Message }); } }

    [HttpGet("~/api/jobs/{jobId:int}/applications"), Authorize(Roles = "Recruiter,Admin")]
    public async Task<IActionResult> ForJob(int jobId, CancellationToken ct)
    {
        try { return Ok(await service.GetForJobAsync(UserId, IsAdmin, jobId, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return ForbidWithMessage(ex.Message); }
    }

    [HttpPatch("{applicationId:int}/status"), Authorize(Roles = "Recruiter,Admin")]
    public async Task<IActionResult> UpdateStatus(int applicationId, UpdateApplicationStatusRequest request, CancellationToken ct)
    {
        try { var result = await service.UpdateStatusAsync(UserId, IsAdmin, applicationId, request.Status, ct); return result is null ? NotFound() : Ok(result); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return ForbidWithMessage(ex.Message); }
    }

    private ObjectResult ForbidWithMessage(string message) => StatusCode(StatusCodes.Status403Forbidden, new { message });
}
