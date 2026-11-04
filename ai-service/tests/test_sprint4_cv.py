from app.cv_parser import analyze_text


def test_analyze_text_extracts_skills_and_years():
    result = analyze_text("Software Engineer. Python, SQL Server and React. 3+ years of experience. Bachelor degree.")
    assert "python" in result["skills"]
    assert "sql server" in result["skills"]
    assert result["yearsExperience"] == 3
    assert result["education"] is not None
    assert result["parserVersion"] == "cv-text-extractor-v1"
