using System.Text.Json;
using AIRecruitment.Application.DTOs;
using AIRecruitment.Application.Interfaces;
using AIRecruitment.Domain.Entities;
using AIRecruitment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruitment.Infrastructure.Services;

// ============== 
// TV5 — Phân tích CV, lưu CVAnalysis/MatchResult và truy vết phiên bản AI.
// ==============
public sealed class CvAnalysisService(ApplicationDbContext db, IMLServiceClient mlClient) : ICvAnalysisService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<CVAnalysisDto> AnalyzeMineAsync(int userId, int cvId, CancellationToken ct)
    {
        var cv = await GetOwnedCvAsync(userId, cvId, ct);
        var record = new CVAnalysis { CVId = cv.Id, ParserVersion = "pending", Status = "Processing" };
        db.CVAnalyses.Add(record); await db.SaveChangesAsync(ct);
        try
        {
            var parsed = await mlClient.AnalyzeCvAsync(cv.StoragePath, cv.OriginalFileName, ct);
            record.ParserVersion = parsed.ParserVersion;
            record.ExtractedSkillsJson = JsonSerializer.Serialize(parsed.Skills, JsonOptions);
            record.YearsExperience = parsed.YearsExperience;
            record.Education = parsed.Education;
            record.Status = "Succeeded";
            record.CompletedAtUtc = DateTime.UtcNow;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (TaskCanceledException)
        {
            record.Status = "Failed"; record.ErrorCode = "AI_TIMEOUT"; record.ParserVersion = "unavailable"; record.CompletedAtUtc = DateTime.UtcNow;
        }
        catch (HttpRequestException ex)
        {
            record.Status = "Failed";
            record.ErrorCode = ex.StatusCode.HasValue && (int)ex.StatusCode.Value is >= 400 and < 500 ? "CV_PARSE_FAILED" : "AI_SERVICE_UNAVAILABLE";
            record.ParserVersion = "unavailable"; record.CompletedAtUtc = DateTime.UtcNow;
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or System.Text.Json.JsonException)
        {
            record.Status = "Failed"; record.ErrorCode = "AI_INVALID_RESPONSE"; record.ParserVersion = "unavailable"; record.CompletedAtUtc = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
        return ToDto(record);
    }

    public async Task<MatchResultDto> MatchMineAsync(int userId, int cvId, int jobId, CancellationToken ct)
    {
        var cv = await GetOwnedCvAsync(userId, cvId, ct);
        var job = await db.Jobs.AsNoTracking().SingleOrDefaultAsync(x => x.Id == jobId && x.IsPublished, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy tin tuyển dụng đang mở.");
        var profile = await db.CandidateProfiles.AsNoTracking().SingleAsync(x => x.Id == cv.CandidateProfileId, ct);
        var analysis = await db.CVAnalyses.AsNoTracking().Where(x => x.CVId == cvId && x.Status == "Succeeded").OrderByDescending(x => x.CreatedAtUtc).FirstOrDefaultAsync(ct);
        CVAnalysisServiceResponse? parsed = null;
        try { parsed = await mlClient.AnalyzeCvAsync(cv.StoragePath, cv.OriginalFileName, ct); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidDataException or IOException) { /* fallback below */ }
        var cvSkills = analysis is not null ? DeserializeSkills(analysis.ExtractedSkillsJson) : profile.SkillsCsv?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() ?? [];
        if (parsed is not null && parsed.Skills.Count > 0) cvSkills = parsed.Skills.ToList();
        var years = parsed?.YearsExperience ?? analysis?.YearsExperience ?? profile.YearsExperience ?? 0;
        MatchingServiceResponse match;
        try
        {
            if (parsed is null) throw new HttpRequestException("AI parser unavailable; use deterministic fallback.");
            match = await mlClient.MatchAsync(new MatchingServiceRequest(cvSkills, years, parsed.ExtractedTextPreview, job.Title, job.Description, job.RequiredSkillsCsv), ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidDataException or IOException or System.Text.Json.JsonException)
        {
            var required = job.RequiredSkillsCsv?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
            var cvSet = cvSkills.Select(Normalize).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var matching = required.Where(s => cvSet.Contains(Normalize(s))).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var missing = required.Where(s => !cvSet.Contains(Normalize(s))).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var score = required.Length == 0 ? 0m : Math.Round((decimal)matching.Length / required.Length * 100m, 2);
            match = new MatchingServiceResponse(score, matching, missing,
                "Fallback theo giao nhau kỹ năng vì AI Service không khả dụng. Điểm này không phải kết quả của mô hình ML.",
                "fallback-rule-v1", new Dictionary<string, double>(), "fallback");
        }

        var version = await db.AIModelVersions.SingleOrDefaultAsync(x => x.Version == match.ModelVersion, ct);
        if (version is null)
        {
            version = new AIModelVersion { Version = match.ModelVersion, Algorithm = match.Mode == "ml" ? "Logistic Regression baseline" : "Rule-based fallback", DatasetDescription = "Xem docs/SPRINT4_AI_INTEGRATION.md; fallback không phải mô hình đã huấn luyện.", MetricsJson = "{}", IsActive = match.Mode == "ml" };
            db.AIModelVersions.Add(version); await db.SaveChangesAsync(ct);
        }
        var result = new MatchResult
        {
            CVId = cvId, JobId = jobId, AIModelVersionId = version.Id,
            MatchScore = Math.Clamp(match.MatchScore, 0m, 100m),
            MatchingSkillsJson = JsonSerializer.Serialize(match.MatchingSkills, JsonOptions),
            MissingSkillsJson = JsonSerializer.Serialize(match.MissingSkills, JsonOptions),
            Explanation = match.Explanation,
            FeatureValuesJson = JsonSerializer.Serialize(match.FeatureValues, JsonOptions),
            ModelVersion = match.ModelVersion
        };
        db.MatchResults.Add(result); await db.SaveChangesAsync(ct);
        return new MatchResultDto(result.Id, result.CVId, result.JobId, job.Title, result.MatchScore, match.MatchingSkills, match.MissingSkills, result.Explanation, result.ModelVersion, result.CreatedAtUtc);
    }

    public async Task<IReadOnlyList<RecommendedJobDto>> GetRecommendationsMineAsync(int userId, int cvId, int topK, CancellationToken ct)
    {
        if (topK is < 1 or > 20) throw new ArgumentOutOfRangeException(nameof(topK), "topK phải trong khoảng 1–20.");
        var cv = await GetOwnedCvAsync(userId, cvId, ct);
        var profile = cv.CandidateProfile ?? throw new InvalidOperationException("CV chưa gắn với hồ sơ ứng viên.");
        var jobs = await db.Jobs.AsNoTracking()
            .Where(x => x.IsPublished)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(200)
            .ToListAsync(ct);
        if (jobs.Count == 0) return [];

        var analysis = await db.CVAnalyses.AsNoTracking()
            .Where(x => x.CVId == cvId && x.Status == "Succeeded")
            .OrderByDescending(x => x.CreatedAtUtc).FirstOrDefaultAsync(ct);
        CVAnalysisServiceResponse? parsed = null;
        try { parsed = await mlClient.AnalyzeCvAsync(cv.StoragePath, cv.OriginalFileName, ct); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidDataException or IOException or System.Text.Json.JsonException)
        { /* Use saved analysis/profile and the explicit fallback when the parser is unavailable. */ }

        var cvSkills = parsed is { Skills.Count: > 0 }
            ? parsed.Skills.ToList()
            : analysis is not null ? DeserializeSkills(analysis.ExtractedSkillsJson)
            : profile.SkillsCsv?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() ?? [];
        var years = parsed?.YearsExperience ?? analysis?.YearsExperience ?? profile.YearsExperience ?? 0;
        var request = new RecommendationServiceRequest(
            cvSkills, years, parsed?.ExtractedTextPreview ?? profile.Summary ?? profile.SkillsCsv ?? string.Empty,
            profile.FullName,
            jobs.Select(job => new RecommendationJobRequest(job.Id, job.Title, job.Description, job.RequiredSkillsCsv,
                null, job.Location ?? string.Empty, job.EmploymentType ?? "FullTime")).ToArray(), topK);

        IReadOnlyList<RecommendationServiceItem> recommendations;
        try
        {
            var response = await mlClient.RecommendAsync(request, ct);
            recommendations = response.Recommendations;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidDataException or IOException or System.Text.Json.JsonException)
        {
            // TV5 — Degraded mode is transparent and still gives a simple skills-overlap ranking.
            var cvSet = cvSkills.Select(Normalize).ToHashSet(StringComparer.OrdinalIgnoreCase);
            recommendations = jobs.Select(job =>
            {
                var required = job.RequiredSkillsCsv?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
                var matched = required.Where(skill => cvSet.Contains(Normalize(skill))).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                var missing = required.Where(skill => !cvSet.Contains(Normalize(skill))).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                var score = required.Length == 0 ? 0m : Math.Round((decimal)matched.Length / required.Length * 100m, 2);
                return new RecommendationServiceItem(job.Id, job.Title, job.Location ?? string.Empty, job.EmploymentType ?? "FullTime",
                    score, matched, missing,
                    "AI Service không khả dụng; thứ hạng fallback chỉ dựa trên giao nhau kỹ năng và cần được xem xét thủ công.",
                    "fallback-rule-v1", new Dictionary<string, double>(), "fallback");
            }).OrderByDescending(x => x.MatchScore).ThenBy(x => x.JobTitle).Take(topK).ToArray();
        }

        var selected = recommendations.OrderByDescending(x => x.MatchScore).ThenBy(x => x.JobTitle).Take(topK).ToList();
        if (selected.Count == 0) return [];
        var versionNames = selected.Select(x => x.ModelVersion).Distinct(StringComparer.Ordinal).ToArray();
        var versions = await db.AIModelVersions.Where(x => versionNames.Contains(x.Version)).ToDictionaryAsync(x => x.Version, StringComparer.Ordinal, ct);
        foreach (var item in selected)
        {
            if (versions.ContainsKey(item.ModelVersion)) continue;
            var version = new AIModelVersion
            {
                Version = item.ModelVersion,
                Algorithm = item.Mode == "ml" ? "Model selected by validation" : "Rule-based fallback",
                DatasetDescription = "Sprint 5 recommendations; see docs/SPRINT5_AI_MATCHING.md.",
                MetricsJson = "{}",
                IsActive = item.Mode == "ml"
            };
            db.AIModelVersions.Add(version);
            versions[item.ModelVersion] = version;
        }
        await db.SaveChangesAsync(ct);

        var resultEntities = selected.Select(item => new MatchResult
        {
            CVId = cvId,
            JobId = item.JobId,
            AIModelVersionId = versions[item.ModelVersion].Id,
            MatchScore = Math.Clamp(item.MatchScore, 0m, 100m),
            MatchingSkillsJson = JsonSerializer.Serialize(item.MatchingSkills, JsonOptions),
            MissingSkillsJson = JsonSerializer.Serialize(item.MissingSkills, JsonOptions),
            Explanation = item.Explanation,
            FeatureValuesJson = JsonSerializer.Serialize(item.FeatureValues, JsonOptions),
            ModelVersion = item.ModelVersion,
            CreatedAtUtc = DateTime.UtcNow
        }).ToList();
        db.MatchResults.AddRange(resultEntities);
        await db.SaveChangesAsync(ct);
        var jobsById = jobs.ToDictionary(x => x.Id);
        return resultEntities.Select((result, index) =>
        {
            var job = jobsById[result.JobId];
            var original = selected[index];
            return new RecommendedJobDto(index + 1, result.Id, result.CVId, result.JobId, job.Title,
                job.Location ?? string.Empty, job.EmploymentType ?? "FullTime", result.MatchScore,
                DeserializeSkills(result.MatchingSkillsJson), DeserializeSkills(result.MissingSkillsJson),
                result.Explanation ?? string.Empty, result.ModelVersion, original.Mode, result.CreatedAtUtc);
        }).ToArray();
    }

    public async Task<IReadOnlyList<MatchResultDto>> GetMatchesMineAsync(int userId, int cvId, CancellationToken ct)
    {
        _ = await GetOwnedCvAsync(userId, cvId, ct);
        var rows = await db.MatchResults.AsNoTracking().Include(x => x.Job).Where(x => x.CVId == cvId).OrderByDescending(x => x.CreatedAtUtc).Take(100).ToListAsync(ct);
        return rows.Select(x => new MatchResultDto(x.Id, x.CVId, x.JobId, x.Job?.Title ?? "(đã xóa)", x.MatchScore, DeserializeSkills(x.MatchingSkillsJson), DeserializeSkills(x.MissingSkillsJson), x.Explanation, x.ModelVersion, x.CreatedAtUtc)).ToArray();
    }

    private async Task<CV> GetOwnedCvAsync(int userId, int cvId, CancellationToken ct)
    {
        var cv = await db.CVs.Include(x => x.CandidateProfile).SingleOrDefaultAsync(x => x.Id == cvId, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy CV.");
        if (cv.CandidateProfile?.UserId != userId) throw new UnauthorizedAccessException("Bạn chỉ được phân tích CV thuộc hồ sơ của mình.");
        if (!File.Exists(cv.StoragePath)) throw new FileNotFoundException("Tệp CV không còn tồn tại trong vùng lưu trữ.");
        return cv;
    }

    private static CVAnalysisDto ToDto(CVAnalysis x) => new(x.Id, x.CVId, x.Status, DeserializeSkills(x.ExtractedSkillsJson), x.YearsExperience, x.Education, x.ErrorCode, x.ParserVersion, x.CreatedAtUtc, x.CompletedAtUtc);
    private static List<string> DeserializeSkills(string? json) { try { return JsonSerializer.Deserialize<List<string>>(json ?? "[]", JsonOptions) ?? []; } catch (JsonException) { return []; } }
    private static string Normalize(string s) => string.Join(' ', s.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
