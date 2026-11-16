# Model artifacts

Generated model artifacts are not committed by default. Train and compare models from the `ai-service` directory:

```bash
python -m app.compare_models
```

The command writes `logistic_regression.joblib`, `random_forest.joblib`, the selected `matching_model.joblib`, and `reports/sprint5_model_comparison.json`. `predict.py` prefers `matching_model.joblib`, falling back to the Sprint 3/4 Logistic Regression artifact for compatibility. The bundled data uses `SYN` identifiers; its metrics are for demonstrating the pipeline only and must not be presented as real-world recruitment performance.
