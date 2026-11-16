from __future__ import annotations
import argparse, json
from pathlib import Path
import pandas as pd
from joblib import load
from sklearn.metrics import classification_report, confusion_matrix, roc_auc_score
from sklearn.model_selection import train_test_split

# ============== 
# TV5 — Re-evaluate saved baseline using the same deterministic holdout split.
# ==============
FEATURES=["skill_overlap","experience_match","title_similarity","description_similarity"]
ROOT=Path(__file__).resolve().parents[1]
def evaluate(data: Path, model_path: Path) -> dict:
    df=pd.read_csv(data)[FEATURES+["label"]].dropna(); X_train,X_test,y_train,y_test=train_test_split(df[FEATURES],df.label.astype(int),test_size=.25,random_state=42,stratify=df.label)
    artifact=load(model_path); model=artifact["pipeline"]; pred=model.predict(X_test); prob=model.predict_proba(X_test)[:,1]
    return {"model_version":artifact["model_version"],"test_rows":len(y_test),"classification_report":classification_report(y_test,pred,labels=[0,1],output_dict=True,zero_division=0),"confusion_matrix":confusion_matrix(y_test,pred,labels=[0,1]).tolist(),"roc_auc":float(roc_auc_score(y_test,prob)) if y_test.nunique()==2 else None,"caveat":"Chỉ hợp lệ cho dataset được chỉ định; không khái quát sang dữ liệu thực tế nếu chưa xác minh nhãn."}
if __name__=="__main__":
 p=argparse.ArgumentParser();p.add_argument('--data',default=str(ROOT/'data'/'processed'/'cv_job_pairs.csv'));p.add_argument('--model',default=str(ROOT/'models'/'logistic_regression.joblib'));a=p.parse_args();print(json.dumps(evaluate(Path(a.data),Path(a.model)),ensure_ascii=False,indent=2))
