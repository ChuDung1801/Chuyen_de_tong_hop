from pydantic import BaseModel, Field

# ============== 
# TV5 — Versioned feature contract shared by training and online inference.
# ==============
class MatchingFeatures(BaseModel):
    skill_overlap: float = Field(ge=0, le=1)
    experience_match: float = Field(ge=0, le=1)
    title_similarity: float = Field(ge=0, le=1)
    description_similarity: float = Field(ge=0, le=1)

class MLPredictRequest(MatchingFeatures):
    cv_id: str | None = None
    job_id: int | None = None

class MLPredictResponse(BaseModel):
    match_probability: float = Field(ge=0, le=1)
    match_score: float = Field(ge=0, le=100)
    model_version: str
    feature_values: dict[str, float]
    note: str
