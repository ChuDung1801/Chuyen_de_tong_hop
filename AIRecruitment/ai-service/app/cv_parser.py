from __future__ import annotations
import io
import re
from pathlib import Path
from docx import Document
from pypdf import PdfReader

# ============== 
# TV2 — Trích xuất văn bản PDF/DOCX; chỉ trả kỹ năng và thông tin cấu trúc cần thiết.
# ==============
SKILL_TERMS = [
    "python", "c#", ".net", "asp.net core", "sql server", "sql", "postgresql",
    "mongodb", "react", "typescript", "javascript", "docker", "kubernetes",
    "machine learning", "scikit-learn", "pandas", "numpy", "fastapi", "entity framework",
    "git", "linux", "azure", "aws", "html", "css", "java", "c++", "node.js",
]
EDUCATION_TERMS = ["bachelor", "bachelor's", "master", "master's", "university", "college", "đại học", "cao đẳng", "thạc sĩ", "cử nhân"]

def extract_text(filename: str, data: bytes) -> str:
    ext = Path(filename).suffix.lower()
    if ext == ".pdf":
        reader = PdfReader(io.BytesIO(data))
        chunks = [(page.extract_text() or "") for page in reader.pages[:30]]
        text = "\n".join(chunks)
    elif ext == ".docx":
        doc = Document(io.BytesIO(data))
        chunks = [p.text for p in doc.paragraphs]
        for table in doc.tables:
            for row in table.rows:
                chunks.append(" ".join(cell.text for cell in row.cells))
        text = "\n".join(chunks)
    else:
        raise ValueError("Định dạng phân tích hỗ trợ: PDF và DOCX. Tệp DOC cũ chưa được hỗ trợ.")
    text = re.sub(r"\s+", " ", text).strip()
    if not text:
        raise ValueError("Không trích xuất được văn bản. PDF scan có thể cần OCR ở phiên bản sau.")
    return text[:12000]

def analyze_text(text: str) -> dict:
    folded = text.casefold()
    skills = [term for term in SKILL_TERMS if term.casefold() in folded]
    exp_matches = re.findall(r"(?<!\d)(\d{1,2})\s*\+?\s*(?:years?|yrs?|năm)\s*(?:of\s+)?(?:experience|kinh nghiệm)?", folded)
    years = max((int(value) for value in exp_matches if int(value) <= 70), default=None)
    education = next((line.strip()[:250] for line in re.split(r"[\n.;]", text) if any(term in line.casefold() for term in EDUCATION_TERMS)), None)
    return {"skills": skills, "yearsExperience": years, "education": education, "parserVersion": "cv-text-extractor-v1", "extractedTextPreview": text[:6000]}
