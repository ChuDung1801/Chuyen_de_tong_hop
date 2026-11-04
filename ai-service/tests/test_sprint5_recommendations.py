from app.sprint5_schemas import RecommendationRequest
from app.recommendations import recommend_jobs


def test_recommendations_sorted_by_match_score(monkeypatch):
    def fake_match(request):
        score = 90.0 if request.job_title == "Python Engineer" else 40.0
        return {
            "matchScore": score,
            "matchingSkills": ["python"] if score > 50 else [],
            "missingSkills": [] if score > 50 else ["python"],
            "explanation": "test explanation",
            "modelVersion": "test-model",
            "featureValues": {"skill_overlap": 1.0 if score > 50 else 0.0},
            "mode": "ml",
        }
    monkeypatch.setattr("app.recommendations.structured_match", fake_match)
    request = RecommendationRequest(
        cvSkills=["Python"], cvYearsExperience=2, cvText="Python engineer",
        jobs=[
            {"jobId": 2, "jobTitle": "Junior Analyst", "requiredSkillsCsv": "SQL"},
            {"jobId": 1, "jobTitle": "Python Engineer", "requiredSkillsCsv": "Python"},
        ], topK=2,
    )
    result = recommend_jobs(request)
    assert [item.job_id for item in result.recommendations] == [1, 2]
    assert result.recommendations[0].match_score == 90.0


def test_recommendations_respects_top_k(monkeypatch):
    monkeypatch.setattr("app.recommendations.structured_match", lambda req: {
        "matchScore": 50.0, "matchingSkills": [], "missingSkills": [],
        "explanation": "x", "modelVersion": "fallback-rule-v1",
        "featureValues": {}, "mode": "fallback",
    })
    jobs = [{"jobId": i, "jobTitle": f"Job {i}"} for i in range(1, 5)]
    result = recommend_jobs(RecommendationRequest(jobs=jobs, topK=2))
    assert len(result.recommendations) == 2
