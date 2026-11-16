from __future__ import annotations
import re
from difflib import SequenceMatcher

# ============== 
# TV5 — Transparent features for CV–Job matching; values are in [0,1].
# ==============
def normalize_text(value: str) -> str:
    return re.sub(r"\s+", " ", (value or "").casefold()).strip()

def normalize_skill(skill: str) -> str:
    return normalize_text(skill)

def overlap(job_skills: list[str], cv_skills: list[str]) -> tuple[list[str], list[str], float]:
    required = {normalize_skill(s) for s in job_skills if s.strip()}
    candidate = {normalize_skill(s) for s in cv_skills if s.strip()}
    matching = sorted(required & candidate); missing = sorted(required - candidate)
    score = len(matching) / len(required) if required else 0.0
    return matching, missing, round(score, 4)

def experience_match(cv_years: float, required_years: float) -> float:
    if required_years <= 0: return 1.0
    return round(min(max(cv_years, 0.0) / required_years, 1.0), 4)

def text_similarity(left: str, right: str) -> float:
    left, right = normalize_text(left), normalize_text(right)
    if not left or not right: return 0.0
    return round(SequenceMatcher(None, left, right).ratio(), 4)

def build_features(cv_skills: list[str], job_skills: list[str], cv_years: float, required_years: float, cv_title: str, job_title: str, cv_text: str, job_text: str) -> dict[str, float]:
    _, _, skill_score = overlap(job_skills, cv_skills)
    return {"skill_overlap": skill_score, "experience_match": experience_match(cv_years, required_years), "title_similarity": text_similarity(cv_title, job_title), "description_similarity": text_similarity(cv_text, job_text)}
