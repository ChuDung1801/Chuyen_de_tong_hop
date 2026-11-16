using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using AIRecruitment.Application.DTOs;
using AIRecruitment.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruitment.Presentation.Controllers;

[ApiController]
[Authorize(Roles = "Candidate,Recruiter")]
[Route("api/recruiter-access-requests")]
public sealed class RecruiterAccessRequestsController(IRecruiterAccessService service) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("me")]
    public async Task<IActionResult> GetMine(CancellationToken ct) => Ok(await service.GetMyLatestAsync(UserId, ct));

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> Submit([FromForm] SubmitRecruiterAccessForm form, CancellationToken ct)
    {
        if (form.Document is null || form.Document.Length == 0)
            return BadRequest(new { message = "Vui lòng tải lên giấy tờ xác minh doanh nghiệp." });

        try
        {
            await using var stream = form.Document.OpenReadStream();
            var request = new SubmitRecruiterAccessRequest(
                form.ApplicantFullName, form.ApplicantPhone, form.ApplicantPosition,
                form.CompanyName, form.TaxCode, form.Website, form.Industry,
                form.CompanyAddress, form.CompanyDescription, form.ApplicantNote);
            var result = await service.SubmitAsync(UserId, request, form.Document.FileName,
                form.Document.ContentType, stream, form.Document.Length, ct);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message }); }
    }
}

public sealed class SubmitRecruiterAccessForm
{
    [Required, StringLength(150, MinimumLength = 2)] public string ApplicantFullName { get; set; } = string.Empty;
    [Required, StringLength(40)] public string ApplicantPhone { get; set; } = string.Empty;
    [Required, StringLength(150)] public string ApplicantPosition { get; set; } = string.Empty;
    [Required, StringLength(200)] public string CompanyName { get; set; } = string.Empty;
    [Required, StringLength(40)] public string TaxCode { get; set; } = string.Empty;
    [StringLength(500)] public string? Website { get; set; }
    [StringLength(150)] public string? Industry { get; set; }
    [Required, StringLength(300)] public string CompanyAddress { get; set; } = string.Empty;
    [StringLength(3000)] public string? CompanyDescription { get; set; }
    [StringLength(2000)] public string? ApplicantNote { get; set; }
    [Required] public IFormFile? Document { get; set; }
}
