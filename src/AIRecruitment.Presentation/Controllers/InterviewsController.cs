using System.Security.Claims;
using AIRecruitment.Application.DTOs;
using AIRecruitment.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruitment.Presentation.Controllers;

// ============== 
// TV4 — Lịch phỏng vấn chỉ được tạo bởi nhà tuyển dụng sở hữu tin tuyển dụng.
// ==============
[ApiController, Authorize, Route("api")]
public sealed class InterviewsController(IApplicationService service) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin => User.IsInRole("Admin");

    [HttpPost("applications/{applicationId:int}/interviews"), Authorize(Roles = "Recruiter,Admin")]
    public async Task<IActionResult> Create(int applicationId, CreateInterviewRequest request, CancellationToken ct)
    {
        try { var result = await service.CreateInterviewAsync(UserId, IsAdmin, applicationId, request, ct); return StatusCode(StatusCodes.Status201Created, result); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message }); }
    }

    [HttpGet("interviews/my"), Authorize(Roles = "Candidate,Recruiter,Admin")]
    public async Task<IActionResult> Mine(CancellationToken ct) => Ok(await service.GetMyInterviewsAsync(UserId, ct));
}
