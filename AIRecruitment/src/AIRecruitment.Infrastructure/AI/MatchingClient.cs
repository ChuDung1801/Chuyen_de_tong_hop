using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AIRecruitment.Application.DTOs;
using AIRecruitment.Application.Interfaces;

namespace AIRecruitment.Infrastructure.AI;

public sealed class MatchingClient(HttpClient httpClient) : IMatchingClient
{
    public async Task<MatchResponse> MatchAsync(MatchRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/v1/match", new
        {
            job_id = request.JobId,
            cv_text = request.CvText,
            skills = request.Skills
        }, cancellationToken);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AiMatchResponse>(cancellationToken: cancellationToken)
                     ?? throw new InvalidOperationException("AI service returned an empty response.");

        return new MatchResponse(result.MatchScore, result.ModelVersion, result.MatchingSkills, result.MissingSkills, result.Explanation);
    }

    private sealed record AiMatchResponse(
        [property: JsonPropertyName("match_score")] decimal MatchScore,
        [property: JsonPropertyName("model_version")] string ModelVersion,
        [property: JsonPropertyName("matching_skills")] string[] MatchingSkills,
        [property: JsonPropertyName("missing_skills")] string[] MissingSkills,
        [property: JsonPropertyName("explanation")] string Explanation);
}
