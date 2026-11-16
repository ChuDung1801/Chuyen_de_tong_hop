from __future__ import annotations
import os
from pathlib import Path
from joblib import load
import numpy as np

FEATURES = ["skill_overlap", "experience_match", "title_similarity", "description_similarity"]
ROOT = Path(__file__).resolve().parents[1]


def predict(features: dict[str, float], model_path: Path | None = None) -> tuple[float, str]:
    configured = os.getenv("MATCHING_MODEL_PATH")
    if model_path is not None:
        path = model_path
    elif configured:
        path = Path(configured)
    elif (ROOT / "models" / "matching_model.joblib").exists():
        path = ROOT / "models" / "matching_model.joblib"
    else:
        # Backward compatibility with the Sprint 3/4 baseline artifact name.
        path = ROOT / "models" / "logistic_regression.joblib"
    if not path.exists():
        raise FileNotFoundError("Chưa có model artifact. Chạy python -m app.compare_models để huấn luyện và đánh giá.")
    artifact = load(path)
    vector = np.array([[float(features[k]) for k in FEATURES]], dtype=float)
    probability = float(artifact["pipeline"].predict_proba(vector)[0][1])
    return probability, str(artifact.get("model_version", "unknown-model"))
