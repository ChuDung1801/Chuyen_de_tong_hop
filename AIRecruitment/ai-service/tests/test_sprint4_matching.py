from app.sprint4_schemas import StructuredMatchRequest
from app.structured_matching import structured_match


def test_structured_match_fallback_is_explicit(monkeypatch):
    def no_model(_features):
        raise FileNotFoundError("model missing")
    monkeypatch.setattr("app.structured_matching.predict", no_model)
    request = StructuredMatchRequest(
        cvSkills=["Python", "SQL Server"], cvYearsExperience=2,
        cvText="Python developer with SQL Server experience",
        jobTitle="Python developer", jobDescription="Build Python services",
        requiredSkillsCsv="Python, SQL Server, Docker", requiredYearsExperience=2,
    )
    result = structured_match(request)
    assert result["modelVersion"] == "fallback-rule-v1"
    assert result["mode"] == "fallback"
    assert {s.casefold() for s in result["matchingSkills"]} == {"python", "sql server"}
    assert "docker" in {s.casefold() for s in result["missingSkills"]}
    assert 0 <= result["matchScore"] <= 100
