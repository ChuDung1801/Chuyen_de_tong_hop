from pydantic import BaseModel, Field

# ============== 
# TV5 — Schema request/response cho bước chuẩn hóa kỹ năng ban đầu.
# ==============
class NormalizeSkillsRequest(BaseModel):
    skills: list[str] = Field(default_factory=list, max_length=200)


class NormalizeSkillsResponse(BaseModel):
    skills: list[str]
    dictionary_version: str = "skills-dictionary-sprint2-v1"
