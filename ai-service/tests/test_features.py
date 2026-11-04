from app.feature_engineering import overlap, experience_match, text_similarity, build_features

def test_overlap():
    matching,missing,score=overlap(["Python","SQL"],["python"])
    assert matching==["python"] and missing==["sql"] and score==0.5

def test_experience_match_clamped():
    assert experience_match(2,4)==0.5 and experience_match(10,4)==1.0

def test_text_similarity_empty():
    assert text_similarity("", "backend engineer")==0.0

def test_feature_bounds():
    values=build_features(["Python"],["Python","SQL"],2,4,"Engineer","Backend Engineer","python api","backend api")
    assert all(0 <= v <= 1 for v in values.values())
