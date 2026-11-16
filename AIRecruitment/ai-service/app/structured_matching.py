from __future__ import annotations
from app.feature_engineering import build_features, overlap
from app.predict import predict


def structured_match(request) -> dict:
    cv_skills = request.cv_skills or []
    job_skills = [s.strip() for s in (request.required_skills_csv or "").split(",") if s.strip()]
    matching, missing, skill_score = overlap(job_skills, cv_skills)
    features = build_features(
        cv_skills, job_skills, request.cv_years_experience,
        request.required_years_experience or 0, request.cv_title or "",
        request.job_title, request.cv_text, request.job_description,
    )
    try:
        probability, version = predict(features)
        mode = "ml"
    except FileNotFoundError:
        probability = (0.60 * features["skill_overlap"] + 0.20 * features["experience_match"] +
                       0.10 * features["title_similarity"] + 0.10 * features["description_similarity"])
        version = "fallback-rule-v1"
        mode = "fallback"
    score = round(max(0.0, min(100.0, probability * 100.0)), 2)

    feature_summary = (
        f"Đặc trưng đầu vào: kỹ năng {features['skill_overlap']:.0%}, "
        f"kinh nghiệm {features['experience_match']:.0%}, "
        f"tương đồng tiêu đề {features['title_similarity']:.0%}, "
        f"tương đồng nội dung {features['description_similarity']:.0%}."
    )
    if not job_skills:
        reason = "Tin tuyển dụng chưa khai báo kỹ năng bắt buộc; kết quả cần được xem xét thủ công."
    else:
        reason = f"Kỹ năng trùng: {len(matching)}/{len(job_skills)}; kỹ năng còn thiếu: {len(missing)}."
    method = "Điểm từ mô hình đã huấn luyện." if mode == "ml" else "Chưa có model artifact; đang dùng fallback theo trọng số, không phải dự đoán ML."
    explanation = f"{reason} {feature_summary} {method} Các giá trị đặc trưng mô tả đầu vào, không phải mức đóng góp nhân quả của từng yếu tố."
    return {"matchScore": score, "matchingSkills": matching, "missingSkills": missing,
            "explanation": explanation, "modelVersion": version, "featureValues": features, "mode": mode}
