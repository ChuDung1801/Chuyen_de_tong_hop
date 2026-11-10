using System.Security.Claims;
using AIRecruitment.Application.DTOs;
using AIRecruitment.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruitment.Presentation.Controllers;

// ============== 
// TV3 — Recruiter chỉ xem công ty mình sở hữu.
// ==============
[ApiController, Authorize(Roles = "Recruiter,Admin"), Route("api/companies")]
public sealed class CompaniesController(ICompanyJobService service) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("my")] public async Task<IActionResult> Mine(CancellationToken ct) => Ok(await service.GetMyCompaniesAsync(UserId, ct));
    [HttpPost] public async Task<IActionResult> Create(CreateCompanyRequest request, CancellationToken ct)
    { try { var result = await service.CreateCompanyAsync(UserId, request, ct); return CreatedAtAction(nameof(Mine), result); } catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); } }
}
