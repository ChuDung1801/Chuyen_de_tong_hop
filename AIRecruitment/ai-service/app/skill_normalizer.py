from __future__ import annotations

import csv
import os
from pathlib import Path

# ============== 
# TV5 — Chuẩn hóa tên kỹ năng bằng dictionary versioned, không khẳng định đây là mô hình ML.
# ==============
def _dictionary_path() -> Path:
    configured = os.getenv("SKILLS_DICTIONARY_PATH")
    if configured:
        return Path(configured)
    return Path(__file__).resolve().parents[1] / "data" / "reference" / "skills_dictionary.csv"


def load_alias_map(path: Path | None = None) -> dict[str, str]:
    path = path or _dictionary_path()
    aliases: dict[str, str] = {}
    if not path.exists():
        return aliases
    with path.open("r", encoding="utf-8-sig", newline="") as stream:
        for row in csv.DictReader(stream):
            canonical = (row.get("canonical") or "").strip()
            if not canonical:
                continue
            aliases[canonical.casefold()] = canonical
            for alias in (row.get("aliases") or "").split("|"):
                if alias.strip():
                    aliases[alias.strip().casefold()] = canonical
    return aliases


def normalize_skills(values: list[str], alias_map: dict[str, str] | None = None) -> list[str]:
    alias_map = alias_map if alias_map is not None else load_alias_map()
    normalized: dict[str, str] = {}
    for value in values:
        candidate = " ".join(value.strip().split())
        if not candidate:
            continue
        canonical = alias_map.get(candidate.casefold(), candidate)
        normalized[canonical.casefold()] = canonical
    return sorted(normalized.values(), key=str.casefold)
