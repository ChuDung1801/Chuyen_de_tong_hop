using AIRecruitment.Application.DTOs;

namespace AIRecruitment.Application.Interfaces;

public interface IMatchingClient
{
    Task<MatchResponse> MatchAsync(MatchRequest request, CancellationToken cancellationToken = default);
}
