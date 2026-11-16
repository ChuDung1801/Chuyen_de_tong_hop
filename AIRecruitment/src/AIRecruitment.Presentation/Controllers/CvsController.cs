using System.Security.Claims;
using AIRecruitment.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruitment.Presentation.Controllers;

// ============== 
// TV2 — Upload CV; không công khai đường dẫn vật lý của file.
// ==============
[ApiController, Authorize(Roles = "Candidate"), Route("api/cvs")]
public sealed class CvsController(IProfileService profiles) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("my")]
    public async Task<IActionResult> Mine(CancellationToken ct) => Ok(await profiles.GetMyCvsAsync(UserId, ct));

    [HttpPost]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest(new { message = "Hãy chọn tệp CV." });
        try
        {
            await using var stream = file.OpenReadStream();
            var result = await profiles.UploadCvAsync(UserId, file.FileName, file.ContentType, stream, file.Length, ct);
            return CreatedAtAction(nameof(Mine), result);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message }); }
    }
}
