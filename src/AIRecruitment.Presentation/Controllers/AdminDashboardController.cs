using AIRecruitment.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIRecruitment.Presentation.Controllers;

// ============== TV1 — Sprint 6: dashboard tổng hợp, chỉ dành cho role Admin. ==============
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin")]
public sealed class AdminDashboardController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet("dashboard")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // Return aggregates only. Do not expose emails, CV contents or personal profile fields.
        var totalUsers = await db.Users.AsNoTracking().CountAsync(cancellationToken);
        var candidates = await db.Users.AsNoTracking().CountAsync(x => x.Role == "Candidate", cancellationToken);
        var recruiters = await db.Users.AsNoTracking().CountAsync(x => x.Role == "Recruiter", cancellationToken);
        var totalJobs = await db.Jobs.AsNoTracking().CountAsync(cancellationToken);
        var publishedJobs = await db.Jobs.AsNoTracking().CountAsync(x => x.IsPublished, cancellationToken);
        var totalApplications = await db.JobApplications.AsNoTracking().CountAsync(cancellationToken);
        var applicationsByStatus = await db.JobApplications.AsNoTracking()
            .GroupBy(x => x.Status)
            .Select(group => new { status = group.Key, count = group.Count() })
            .OrderBy(item => item.status)
            .ToListAsync(cancellationToken);
        var totalCvs = await db.CVs.AsNoTracking().CountAsync(cancellationToken);
        var completedCvAnalyses = await db.CVAnalyses.AsNoTracking()
            .CountAsync(x => x.Status == "Completed" || x.Status == "Succeeded", cancellationToken);
        var failedCvAnalyses = await db.CVAnalyses.AsNoTracking()
            .CountAsync(x => x.Status == "Failed", cancellationToken);
        var totalMatches = await db.MatchResults.AsNoTracking().CountAsync(cancellationToken);
        var averageMatchScore = await db.MatchResults.AsNoTracking()
            .Select(x => (double?)x.MatchScore)
            .AverageAsync(cancellationToken) ?? 0d;
        var totalInterviews = await db.Interviews.AsNoTracking().CountAsync(cancellationToken);
        var upcomingInterviews = await db.Interviews.AsNoTracking()
            .CountAsync(x => x.ScheduledAtUtc >= now && x.Status == "Scheduled", cancellationToken);
        var activeModels = await db.AIModelVersions.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new { version = x.Version, algorithm = x.Algorithm, createdAtUtc = x.CreatedAtUtc })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            generatedAtUtc = now,
            users = new { total = totalUsers, candidates, recruiters },
            jobs = new { total = totalJobs, published = publishedJobs },
            applications = new { total = totalApplications, byStatus = applicationsByStatus },
            cvs = new { total = totalCvs, analysesCompleted = completedCvAnalyses, analysesFailed = failedCvAnalyses },
            interviews = new { total = totalInterviews, upcoming = upcomingInterviews },
            ai = new { totalMatches, averageMatchScore = Math.Round(averageMatchScore, 2), activeModelCount = activeModels.Count },
            activeModels
        });
    }
}
