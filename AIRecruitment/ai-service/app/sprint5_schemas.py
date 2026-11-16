from __future__ import annotations
from pydantic import Field
from app.sprint4_schemas import CamelCaseModel


class RecommendationJob(CamelCaseModel):
    job_id: int = Field(alias="jobId", ge=1)
    job_title: str = Field(alias="jobTitle", min_length=1, max_length=200)
    job_description: str = Field(default="", alias="jobDescription", max_length=8000)
    required_skills_csv: str | None = Field(default=None, alias="requiredSkillsCsv", max_length=2000)
    required_years_experience: int | None = Field(default=None, alias="requiredYearsExperience", ge=0, le=70)
    location: str = Field(default="", max_length=200)
    employment_type: str = Field(default="FullTime", alias="employmentType", max_length=50)


class RecommendationRequest(CamelCaseModel):
    cv_skills: list[str] = Field(default_factory=list, alias="cvSkills", max_length=100)
    cv_years_experience: int = Field(default=0, alias="cvYearsExperience", ge=0, le=70)
    cv_text: str = Field(default="", alias="cvText", max_length=12000)
    cv_title: str | None = Field(default=None, alias="cvTitle", max_length=200)
    jobs: list[RecommendationJob] = Field(min_length=1, max_length=200)
    top_k: int = Field(default=5, alias="topK", ge=1, le=20)


class RecommendationItem(CamelCaseModel):
    job_id: int = Field(alias="jobId")
    job_title: str = Field(alias="jobTitle")
    location: str = ""
    employment_type: str = Field(default="FullTime", alias="employmentType")
    match_score: float = Field(alias="matchScore", ge=0, le=100)
    matching_skills: list[str] = Field(alias="matchingSkills")
    missing_skills: list[str] = Field(alias="missingSkills")
    explanation: str
    model_version: str = Field(alias="modelVersion")
    feature_values: dict[str, float] = Field(alias="featureValues")
    mode: str


class RecommendationResponse(CamelCaseModel):
    recommendations: list[RecommendationItem]
