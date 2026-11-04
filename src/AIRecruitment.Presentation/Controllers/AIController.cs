using AIRecruitment.Application.DTOs;
using AIRecruitment.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruitment.Presentation.Controllers;

[ApiController]
[Route("api/ai")]
public sealed class AIController(IMatchingClient matchingClient) : ControllerBase
{
    [HttpPost("match")]
    public async Task<ActionResult<MatchResponse>> Match([FromBody] MatchRequest request, CancellationToken ct)
    {
        if (request.JobId <= 0 || string.IsNullOrWhiteSpace(request.CvText))
            return BadRequest(new { error = "jobId must be positive and cvText is required." });

        try
        {
            return Ok(await matchingClient.MatchAsync(request, ct));
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "AI Service is currently unavailable. Please try again later." });
        }
        catch (TaskCanceledException)
        {
            return StatusCode(StatusCodes.Status504GatewayTimeout,
                new { error = "AI Service request timed out." });
        }
    }
}
