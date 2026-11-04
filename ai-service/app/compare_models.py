from __future__ import annotations
import argparse
import json
from pathlib import Path
from typing import Any

import pandas as pd
from joblib import dump
from sklearn.ensemble import RandomForestClassifier
from sklearn.linear_model import LogisticRegression
from sklearn.metrics import accuracy_score, confusion_matrix, f1_score, precision_score, recall_score, roc_auc_score
from sklearn.model_selection import GroupShuffleSplit, train_test_split
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import StandardScaler

FEATURES = ["skill_overlap", "experience_match", "title_similarity", "description_similarity"]
ROOT = Path(__file__).resolve().parents[1]


def compute_metrics(model: Any, X: pd.DataFrame, y: pd.Series) -> dict:
    predicted = model.predict(X)
    probabilities = model.predict_proba(X)[:, 1]
    return {
        "rows": int(len(y)),
        "accuracy": round(float(accuracy_score(y, predicted)), 4),
        "precision": round(float(precision_score(y, predicted, zero_division=0)), 4),
        "recall": round(float(recall_score(y, predicted, zero_division=0)), 4),
        "f1": round(float(f1_score(y, predicted, zero_division=0)), 4),
        "confusion_matrix_labels_0_1": confusion_matrix(y, predicted, labels=[0, 1]).tolist(),
        "roc_auc": round(float(roc_auc_score(y, probabilities)), 4) if y.nunique() == 2 else None,
    }


def compare_models(data_path: Path, models_dir: Path, report_path: Path, random_state: int = 42) -> dict:
    df = pd.read_csv(data_path)
    missing = set(FEATURES + ["label"]) - set(df.columns)
    if missing:
        raise ValueError(f"Thiếu các cột bắt buộc: {sorted(missing)}")
    group_split = "cv_id" in df.columns
    columns = (["cv_id"] if group_split else []) + FEATURES + ["label"]
    df = df[columns].dropna().reset_index(drop=True)
    labels = df["label"].astype(int)
    if set(labels.unique()) != {0, 1}:
        raise ValueError("Cột label phải gồm cả hai lớp 0 và 1.")
    if len(df) < 30 or labels.value_counts().min() < 5:
        raise ValueError("Cần ít nhất 30 dòng và tối thiểu 5 mẫu cho mỗi lớp để chia train/validation/test.")

    X = df[FEATURES]
    if group_split:
        # Keep rows with the same CV together to reduce train/test leakage.
        groups = df["cv_id"].astype(str)
        split_found = False
        for seed in range(random_state, random_state + 100):
            dev_idx, test_idx = next(GroupShuffleSplit(n_splits=1, test_size=0.20, random_state=seed).split(X, labels, groups))
            dev_groups = groups.iloc[dev_idx]
            tr_local_idx, val_local_idx = next(GroupShuffleSplit(n_splits=1, test_size=0.25, random_state=seed).split(X.iloc[dev_idx], labels.iloc[dev_idx], dev_groups))
            train_idx, validation_idx = dev_idx[tr_local_idx], dev_idx[val_local_idx]
            partitions = [labels.iloc[train_idx], labels.iloc[validation_idx], labels.iloc[test_idx]]
            if all(part.nunique() == 2 and len(part) >= 2 for part in partitions):
                split_found = True
                break
        if not split_found:
            raise ValueError("Không tìm được train/validation/test có đủ hai lớp trong khi giữ toàn bộ các cặp của một CV ở cùng tập.")
        X_train, X_validation, X_test = X.iloc[train_idx], X.iloc[validation_idx], X.iloc[test_idx]
        y_train, y_validation, y_test = labels.iloc[train_idx], labels.iloc[validation_idx], labels.iloc[test_idx]
        cv_groups = {"train": sorted(groups.iloc[train_idx].unique().tolist()),
                     "validation": sorted(groups.iloc[validation_idx].unique().tolist()),
                     "test": sorted(groups.iloc[test_idx].unique().tolist())}
    else:
        # Fallback when the dataset has no group identifier.
        X_dev, X_test, y_dev, y_test = train_test_split(X, labels, test_size=0.20,
            random_state=random_state, stratify=labels)
        X_train, X_validation, y_train, y_validation = train_test_split(X_dev, y_dev,
            test_size=0.25, random_state=random_state, stratify=y_dev)
        cv_groups = None

    candidates = {
        "logistic-regression": Pipeline([
            ("scale", StandardScaler()),
            ("classifier", LogisticRegression(max_iter=1000, class_weight="balanced", random_state=random_state)),
        ]),
        "random-forest": RandomForestClassifier(
            n_estimators=300, min_samples_leaf=2, class_weight="balanced_subsample",
            random_state=random_state, n_jobs=-1,
        ),
    }
    validation_metrics = {}
    for name, model in candidates.items():
        model.fit(X_train, y_train)
        validation_metrics[name] = compute_metrics(model, X_validation, y_validation)

    # Selection uses validation F1, then recall, then precision; never the test set.
    selected_name = max(candidates, key=lambda name: (
        validation_metrics[name]["f1"], validation_metrics[name]["recall"],
        validation_metrics[name]["precision"], name == "logistic-regression",
    ))

    # Refit both candidates on train+validation after choosing by validation; test remains untouched.
    X_development = pd.concat([X_train, X_validation], axis=0)
    y_development = pd.concat([y_train, y_validation], axis=0)
    for model in candidates.values():
        model.fit(X_development, y_development)

    models_dir.mkdir(parents=True, exist_ok=True)
    reports_dir = report_path.parent
    reports_dir.mkdir(parents=True, exist_ok=True)
    test_metrics = {}
    model_versions = {
        "logistic-regression": "logistic-regression-v2",
        "random-forest": "random-forest-v1",
    }
    for name, model in candidates.items():
        test_metrics[name] = compute_metrics(model, X_test, y_test)
        artifact = {"pipeline": model, "model_version": model_versions[name], "features": FEATURES,
                    "algorithm": name, "selected_by_validation": name == selected_name}
        dump(artifact, models_dir / ("logistic_regression.joblib" if name == "logistic-regression" else "random_forest.joblib"))

    selected_artifact = {"pipeline": candidates[selected_name], "model_version": model_versions[selected_name],
                         "features": FEATURES, "algorithm": selected_name,
                         "selected_by_validation": True}
    dump(selected_artifact, models_dir / "matching_model.joblib")
    report = {
        "report_version": "sprint5-model-comparison-v1",
        "dataset_path": str(data_path),
        "rows_used": int(len(df)),
        "split": {"train": int(len(X_train)), "validation": int(len(X_validation)), "test": int(len(X_test)), "random_state": random_state,
                  "group_split_by_cv_id": group_split, "groups": cv_groups},
        "features": FEATURES,
        "selection_rule": "Validation F1, then Recall, then Precision; test split is not used to choose the model.",
        "selected_model_by_validation": selected_name,
        "validation_metrics": validation_metrics,
        "test_metrics_for_reporting_only": test_metrics,
        "caveat": "Dataset hiện có các mã SYN và có tính chất dữ liệu minh họa/tổng hợp. Các metric kiểm tra pipeline, không chứng minh hiệu năng trên CV thực tế.",
        "human_review": "Điểm matching là thông tin tham khảo; không tự động quyết định tuyển dụng.",
    }
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    return report


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Compare Logistic Regression and Random Forest on a held-out dataset.")
    parser.add_argument("--data", type=Path, default=ROOT / "data" / "processed" / "cv_job_pairs.csv")
    parser.add_argument("--models-dir", type=Path, default=ROOT / "models")
    parser.add_argument("--report", type=Path, default=ROOT / "reports" / "sprint5_model_comparison.json")
    parser.add_argument("--random-state", type=int, default=42)
    args = parser.parse_args()
    print(json.dumps(compare_models(args.data, args.models_dir, args.report, args.random_state), ensure_ascii=False, indent=2))
