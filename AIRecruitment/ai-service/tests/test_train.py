from pathlib import Path
from app.train import train

def test_train_creates_artifacts(tmp_path):
    data=Path(__file__).resolve().parents[1]/'data'/'processed'/'cv_job_pairs.csv'
    result=train(data,tmp_path/'model.joblib',tmp_path/'metrics.json')
    assert result['rows_used'] >= 12
    assert (tmp_path/'model.joblib').exists() and (tmp_path/'metrics.json').exists()
