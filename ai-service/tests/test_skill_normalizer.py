from app.skill_normalizer import normalize_skills

# ============== 
# TV5 — Unit tests cho alias, trim và loại bỏ duplicate.
# ==============
def test_aliases_are_normalized_and_deduplicated():
    aliases = {"c sharp": "C#", "c#": "C#", "reactjs": "React"}
    assert normalize_skills([" c sharp ", "C#", "ReactJS", "  "], aliases) == ["C#", "React"]
