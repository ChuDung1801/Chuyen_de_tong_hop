using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AIRecruitment.Application.DTOs;
using AIRecruitment.Application.Interfaces;

namespace AIRecruitment.Infrastructure.AI;

// ============== 
// TV5 — HTTP adapter cho Python ML Service; timeout được cấu hình trong DI.
// ==============
public sealed class MLServiceClient(HttpClient httpClient) : IMLServiceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<CVAnalysisServiceResponse> AnalyzeCvAsync(string filePath, string fileName, CancellationToken ct)
    {
        await using var file = File.OpenRead(filePath);
        using var multipart = new MultipartFormDataContent();
        using var streamContent = new StreamContent(file);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(
            Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".doc" => "application/msword",
                _ => "application/octet-stream"
            });
        multipart.Add(streamContent, "file", Path.GetFileName(fileName));
        using var response = await httpClient.PostAsync("/api/v1/cv/analyze-file", multipart, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CVAnalysisServiceResponse>(JsonOptions, ct)
            ?? throw new InvalidDataException("AI Service trả về response phân tích CV rỗng.");
    }

    public async Task<MatchingServiceResponse> MatchAsync(MatchingServiceRequest request, CancellationToken ct)
    {
        using var response = await httpClient.PostAsJsonAsync("/api/v1/match/structured", request, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MatchingServiceResponse>(JsonOptions, ct)
            ?? throw new InvalidDataException("AI Service trả về response matching rỗng.");
    }

    public async Task<RecommendationServiceResponse> RecommendAsync(RecommendationServiceRequest request, CancellationToken ct)
    {
        using var response = await httpClient.PostAsJsonAsync("/api/v1/recommendations", request, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RecommendationServiceResponse>(JsonOptions, ct)
            ?? throw new InvalidDataException("AI Service trả về response đề xuất rỗng.");
    }
}
