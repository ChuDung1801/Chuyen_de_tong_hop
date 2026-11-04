from pydantic import BaseModel, ConfigDict, Field

# ============== 
# TV5 — Contract camelCase giữa ASP.NET Core và FastAPI.
# ==============
class CamelCaseModel(BaseModel):
    model_config = ConfigDict(populate_by_name=True, serialize_by_alias=True)

class CVAnalysisResponse(CamelCaseModel):
    skills: list[str]
    years_experience: int | None = Field(default=None, alias="yearsExperience")
    education: str | None = None
    parser_version: str = Field(alias="parserVersion")
    extracted_text_preview: str = Field(alias="extractedTextPreview")

class StructuredMatchRequest(CamelCaseModel):
    cv_skills: list[str] = Field(alias="cvSkills")
    cv_years_experience: int = Field(default=0, alias="cvYearsExperience", ge=0, le=70)
    cv_text: str = Field(default="", alias="cvText", max_length=12000)
    job_title: str = Field(alias="jobTitle", min_length=1, max_length=200)
    job_description: str = Field(default="", alias="jobDescription", max_length=8000)
    required_skills_csv: str | None = Field(default=None, alias="requiredSkillsCsv", max_length=2000)
    required_years_experience: int | None = Field(default=None, alias="requiredYearsExperience", ge=0, le=70)
    cv_title: str | None = Field(default=None, alias="cvTitle", max_length=200)

class StructuredMatchResponse(CamelCaseModel):
    match_score: float = Field(alias="matchScore", ge=0, le=100)
    matching_skills: list[str] = Field(alias="matchingSkills")
    missing_skills: list[str] = Field(alias="missingSkills")
    explanation: str
    model_version: str = Field(alias="modelVersion")
    feature_values: dict[str, float] = Field(alias="featureValues")
    mode: str
