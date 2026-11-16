# AI Service

FastAPI service scaffold for Sprint 1.

Run locally:
```bash
python -m venv .venv
pip install -r requirements.txt
uvicorn app.main:app --reload --port 8000
```

- `GET /health`
- `POST /api/v1/match`
- Interactive docs: `/docs`

**Important:** current matching endpoint is a transparent rule-based demonstration using a tiny hard-coded sample skill set. It is not a trained ML model, not a validated score, and must not be used as an automated hiring decision. Sprint 3–5 should replace this with documented feature engineering, a permitted dataset, labeled CV–Job pairs, train/validation/test splits and model evaluation.
