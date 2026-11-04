from fastapi.testclient import TestClient

from app.main import app

client = TestClient(app)


def test_health_endpoint_is_machine_readable():
    response = client.get("/health")
    assert response.status_code == 200
    body = response.json()
    assert body["status"] == "ok"
    assert body["service"] == "ai-service"
    assert isinstance(body["matching_model_available"], bool)


def test_cv_upload_rejects_unsupported_extension():
    response = client.post(
        "/api/v1/cv/analyze-file",
        files={"file": ("payload.exe", b"not a CV", "application/octet-stream")},
    )
    assert response.status_code == 415


def test_cv_upload_rejects_files_over_five_mib():
    oversized = b"0" * (5 * 1024 * 1024 + 1)
    response = client.post(
        "/api/v1/cv/analyze-file",
        files={"file": ("oversized.pdf", oversized, "application/pdf")},
    )
    assert response.status_code == 413


def test_structured_match_endpoint_returns_explainable_contract(monkeypatch):
    monkeypatch.setattr(
        "app.main.structured_match",
        lambda _request: {
            "matchScore": 78.5,
            "matchingSkills": ["Python"],
            "missingSkills": ["Docker"],
            "explanation": "Một kỹ năng trùng khớp; còn thiếu Docker.",
            "modelVersion": "test-fallback-v1",
            "featureValues": {"skill_overlap": 0.5},
            "mode": "fallback",
        },
    )
    response = client.post(
        "/api/v1/match/structured",
        json={
            "cvSkills": ["Python"],
            "cvYearsExperience": 2,
            "cvText": "Python developer",
            "jobTitle": "Backend Developer",
            "jobDescription": "Build APIs",
            "requiredSkillsCsv": "Python, Docker",
        },
    )
    assert response.status_code == 200
    body = response.json()
    assert body["matchScore"] == 78.5
    assert body["matchingSkills"] == ["Python"]
    assert body["missingSkills"] == ["Docker"]
    assert body["mode"] == "fallback"
    assert body["modelVersion"] == "test-fallback-v1"


def test_recommendation_endpoint_rejects_top_k_out_of_range():
    response = client.post(
        "/api/v1/recommendations",
        json={
            "cvSkills": ["Python"],
            "cvYearsExperience": 1,
            "jobs": [{"jobId": 1, "jobTitle": "Python Developer"}],
            "topK": 100,
        },
    )
    assert response.status_code == 422


def test_recommendation_endpoint_rejects_empty_job_list():
    response = client.post(
        "/api/v1/recommendations",
        json={"cvSkills": ["Python"], "jobs": [], "topK": 5},
    )
    assert response.status_code == 422
