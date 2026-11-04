from __future__ import annotations
import argparse, json
from pathlib import Path
import pandas as pd
from joblib import dump
from sklearn.linear_model import LogisticRegression
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import StandardScaler
from sklearn.metrics import accuracy_score, precision_score, recall_score, f1_score, confusion_matrix, roc_auc_score
from sklearn.model_selection import train_test_split

# ============== 
# TV5 — Reproducible Logistic Regression baseline; output metrics, never claim synthetic data as real.
# ==============
FEATURES = ["skill_overlap", "experience_match", "title_similarity", "description_similarity"]
ROOT = Path(__file__).resolve().parents[1]

def train(data_path: Path, model_path: Path, metrics_path: Path, test_size: float = 0.25, random_state: int = 42) -> dict:
    df = pd.read_csv(data_path)
    missing = set(FEATURES + ["label"]) - set(df.columns)
    if missing: raise ValueError(f"Thiếu cột bắt buộc: {sorted(missing)}")
    df = df[FEATURES + ["label"]].dropna()
    if not set(df["label"].unique()).issubset({0, 1}) or df["label"].nunique() != 2: raise ValueError("label phải gồm cả hai lớp 0 và 1")
    if len(df) < 12: raise ValueError("Cần tối thiểu 12 cặp dữ liệu có nhãn để chia tập thử nghiệm")
    X_train, X_test, y_train, y_test = train_test_split(df[FEATURES], df["label"].astype(int), test_size=test_size, random_state=random_state, stratify=df["label"])
    model = Pipeline([("scale", StandardScaler()), ("classifier", LogisticRegression(max_iter=1000, class_weight="balanced", random_state=random_state))])
    model.fit(X_train, y_train)
    pred = model.predict(X_test); probs = model.predict_proba(X_test)[:, 1]
    metrics = {"model_version":"logistic-regression-baseline-v1", "dataset_path":str(data_path), "rows_used":int(len(df)), "train_rows":int(len(X_train)), "test_rows":int(len(X_test)), "features":FEATURES, "accuracy":round(float(accuracy_score(y_test,pred)),4), "precision":round(float(precision_score(y_test,pred,zero_division=0)),4), "recall":round(float(recall_score(y_test,pred,zero_division=0)),4), "f1":round(float(f1_score(y_test,pred,zero_division=0)),4), "confusion_matrix":confusion_matrix(y_test,pred,labels=[0,1]).tolist(), "roc_auc":round(float(roc_auc_score(y_test,probs)),4) if len(set(y_test))==2 else None, "caveat":"Chỉ phản ánh dataset đầu vào. Nếu dataset là dữ liệu minh họa/tổng hợp thì metrics không đại diện hiệu năng thực tế."}
    model_path.parent.mkdir(parents=True,exist_ok=True); metrics_path.parent.mkdir(parents=True,exist_ok=True)
    dump({"pipeline":model,"model_version":metrics["model_version"],"features":FEATURES},model_path)
    metrics_path.write_text(json.dumps(metrics,ensure_ascii=False,indent=2),encoding="utf-8")
    return metrics

if __name__ == "__main__":
    parser=argparse.ArgumentParser(); parser.add_argument("--data",default=str(ROOT/"data"/"processed"/"cv_job_pairs.csv")); parser.add_argument("--model",default=str(ROOT/"models"/"logistic_regression.joblib")); parser.add_argument("--metrics",default=str(ROOT/"reports"/"baseline_metrics.json")); args=parser.parse_args()
    print(json.dumps(train(Path(args.data),Path(args.model),Path(args.metrics)),ensure_ascii=False,indent=2))
