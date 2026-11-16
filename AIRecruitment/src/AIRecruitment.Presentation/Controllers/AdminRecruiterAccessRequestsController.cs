using System.Security.Claims;
using AIRecruitment.Application.DTOs;
using AIRecruitment.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruitment.Presentation.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/recruiter-access-requests")]
public sealed class AdminRecruiterAccessRequestsController(IRecruiterAccessService service) : ControllerBase
{
    private int AdminUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? status, CancellationToken ct)
    {
        try { return Ok(await service.GetForAdminAsync(status, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("{id:int}/document")]
    public async Task<IActionResult> Document(int id, CancellationToken ct)
    {
        var document = await service.GetDocumentForAdminAsync(id, ct);
        if (document is null) return NotFound(new { message = "Không tìm thấy hồ sơ hoặc tài liệu xác minh." });
        Response.Headers.Append("X-Content-Type-Options", "nosniff");
        return File(document.Content, document.ContentType, document.FileName);
    }

    [HttpPost("{id:int}/review")]
    public async Task<IActionResult> Review(int id, ReviewRecruiterAccessRequest request, CancellationToken ct)
    {
        try
        {
            var result = await service.ReviewAsync(AdminUserId, id, request, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }
}
