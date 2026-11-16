from __future__ import annotations
from app.sprint4_schemas import StructuredMatchRequest
from app.sprint5_schemas import RecommendationItem, RecommendationRequest, RecommendationResponse
from app.structured_matching import structured_match


def recommend_jobs(request: RecommendationRequest) -> RecommendationResponse:
    """Rank the supplied jobs by the same explainable matching contract used by Sprint 4."""
    ranked: list[RecommendationItem] = []
    for job in request.jobs:
        result = structured_match(StructuredMatchRequest(
            cvSkills=request.cv_skills,
            cvYearsExperience=request.cv_years_experience,
            cvText=request.cv_text,
            cvTitle=request.cv_title,
            jobTitle=job.job_title,
            jobDescription=job.job_description,
            requiredSkillsCsv=job.required_skills_csv,
            requiredYearsExperience=job.required_years_experience,
        ))
        ranked.append(RecommendationItem(
            jobId=job.job_id,
            jobTitle=job.job_title,
            location=job.location,
            employmentType=job.employment_type,
            matchScore=result["matchScore"],
            matchingSkills=result["matchingSkills"],
            missingSkills=result["missingSkills"],
            explanation=result["explanation"],
            modelVersion=result["modelVersion"],
            featureValues=result["featureValues"],
            mode=result["mode"],
        ))
    ranked.sort(key=lambda item: (-item.match_score, item.job_title.casefold(), item.job_id))
    return RecommendationResponse(recommendations=ranked[:request.top_k])
