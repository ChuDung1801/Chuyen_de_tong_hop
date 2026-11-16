from pydantic import BaseModel, Field

# ============== 
# TV5 — Request/response contract cho demo endpoint tương thích Sprint 2–3.
# ==============
class MatchRequest(BaseModel):
    cv_text: str = Field(default="", max_length=12000)
    skills: list[str] = Field(default_factory=list, max_length=100)

class MatchResponse(BaseModel):
    match_score: float = Field(ge=0, le=100)
    model_version: str
    matching_skills: list[str]
    missing_skills: list[str]
    explanation: str
