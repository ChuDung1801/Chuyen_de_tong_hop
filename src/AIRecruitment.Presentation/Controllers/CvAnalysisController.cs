using System.Security.Claims;
using AIRecruitment.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruitment.Presentation.Controllers;

// ============== 
// TV5 — Candidate kích hoạt phân tích CV và matching; dữ liệu chỉ trả cho chủ CV.
// ==============
[ApiController, Authorize(Roles = "Candidate"), Route("api/cvs")]
public sealed class CvAnalysisController(ICvAnalysisService service) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("{cvId:int}/analyze")]
    public async Task<IActionResult> Analyze(int cvId, CancellationToken ct)
    {
        try { var result = await service.AnalyzeMineAsync(UserId, cvId, ct); return result.Status == "Succeeded" ? Ok(result) : StatusCode(StatusCodes.Status503ServiceUnavailable, result); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message }); }
        catch (FileNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPost("{cvId:int}/match/{jobId:int}")]
    public async Task<IActionResult> Match(int cvId, int jobId, CancellationToken ct)
    {
        try { return Ok(await service.MatchMineAsync(UserId, cvId, jobId, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message }); }
        catch (FileNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("{cvId:int}/matches")]
    public async Task<IActionResult> Matches(int cvId, CancellationToken ct)
    {
        try { return Ok(await service.GetMatchesMineAsync(UserId, cvId, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message }); }
    }

    [HttpGet("{cvId:int}/recommendations")]
    public async Task<IActionResult> Recommendations(int cvId, [FromQuery] int topK = 5, CancellationToken ct = default)
    {
        if (topK is < 1 or > 20) return BadRequest(new { message = "topK phải trong khoảng 1–20." });
        try { return Ok(await service.GetRecommendationsMineAsync(UserId, cvId, topK, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message }); }
        catch (FileNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }
}
