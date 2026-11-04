from pathlib import Path
from fastapi import FastAPI, File, HTTPException, UploadFile
from app.schemas import MatchRequest, MatchResponse
from app.feature_engineering import overlap
from app.skill_normalizer import normalize_skills
from app.skill_schemas import NormalizeSkillsRequest, NormalizeSkillsResponse
from app.ml_schemas import MLPredictRequest, MLPredictResponse
from app.predict import predict
from app.cv_parser import analyze_text, extract_text
from app.sprint4_schemas import CVAnalysisResponse, StructuredMatchRequest, StructuredMatchResponse
from app.structured_matching import structured_match
from app.sprint5_schemas import RecommendationRequest, RecommendationResponse
from app.recommendations import recommend_jobs

# ============== 
# TV5 — API Sprint 3 giữ tương thích; Sprint 4 thêm CV parsing và structured matching.
# ==============
app = FastAPI(title="AI Recruitment ML Service", version="0.4.0", description="CV analysis, skill normalization and explainable CV–Job matching.")

@app.get("/health")
def health():
    models_dir = Path(__file__).resolve().parents[1] / "models"
    return {"status": "ok", "service": "ai-service", "matching_model_available": (models_dir / "matching_model.joblib").exists() or (models_dir / "logistic_regression.joblib").exists()}

@app.post("/api/v1/skills/normalize", response_model=NormalizeSkillsResponse)
def normalize_skills_endpoint(request: NormalizeSkillsRequest):
    return NormalizeSkillsResponse(skills=normalize_skills(request.skills))

@app.post("/api/v1/match", response_model=MatchResponse)
def match(request: MatchRequest):
    cv_skills = normalize_skills(request.skills + [s for s in ["python", "c#", "sql", "react", "typescript", "docker", "asp.net core", "machine learning"] if s in request.cv_text.casefold()])
    demo_job_skills = ["Python", "SQL Server", "Machine Learning"]
    matching, missing, score = overlap([s.casefold() for s in demo_job_skills], [s.casefold() for s in cv_skills])
    return MatchResponse(match_score=score * 100, model_version="demo-rule-v0", matching_skills=matching, missing_skills=missing, explanation="Kết quả rule-based minh họa, không phải dự đoán từ mô hình ML đã huấn luyện.")

@app.post("/api/v1/ml/predict", response_model=MLPredictResponse)
def ml_predict(request: MLPredictRequest):
    features = {k: getattr(request, k) for k in ["skill_overlap", "experience_match", "title_similarity", "description_similarity"]}
    try:
        probability, version = predict(features)
    except FileNotFoundError as exc:
        raise HTTPException(status_code=503, detail=str(exc))
    return MLPredictResponse(match_probability=probability, match_score=round(probability * 100, 2), model_version=version, feature_values=features, note="Điểm là xác suất ước lượng của baseline trên dữ liệu huấn luyện; chỉ dùng tham khảo, không tự động quyết định tuyển dụng.")

@app.post("/api/v1/cv/analyze-file", response_model=CVAnalysisResponse)
async def analyze_cv_file(file: UploadFile = File(...)):
    filename = file.filename or "cv"
    if Path(filename).suffix.lower() not in {".pdf", ".docx"}:
        raise HTTPException(status_code=415, detail="Chỉ hỗ trợ PDF và DOCX; DOC cũ chưa được hỗ trợ.")
    data = await file.read(5 * 1024 * 1024 + 1)
    if len(data) > 5 * 1024 * 1024:
        raise HTTPException(status_code=413, detail="CV vượt giới hạn 5 MB.")
    try:
        text = extract_text(filename, data)
        return analyze_text(text)
    except ValueError as exc:
        raise HTTPException(status_code=422, detail=str(exc))
    except Exception as exc:
        raise HTTPException(status_code=422, detail="Không thể phân tích tệp CV. Hãy kiểm tra tệp có hợp lệ và không bị hỏng.") from exc

@app.post("/api/v1/match/structured", response_model=StructuredMatchResponse)
def structured_match_endpoint(request: StructuredMatchRequest):
    return structured_match(request)

@app.post("/api/v1/recommendations", response_model=RecommendationResponse)
def recommendations_endpoint(request: RecommendationRequest):
    """Rank up to 200 published-job candidates; scores are advisory, not hiring decisions."""
    return recommend_jobs(request)
