"""Feed manifest (``public/.feeds.json``): basename -> output name.

Used to distinguish intentional removals (source file gone) from transient per-feed
errors. See contracts/publishing.md.
"""

from __future__ import annotations

import json
from pathlib import Path

from .render import atomic_write


def read_manifest(path: str | Path) -> dict[str, str]:
    """Read the manifest; return {} if missing or unreadable/invalid."""
    p = Path(path)
    if not p.exists():
        return {}
    try:
        data = json.loads(p.read_text(encoding="utf-8"))
    except (json.JSONDecodeError, OSError):
        return {}
    if not isinstance(data, dict):
        return {}
    return {str(k): str(v) for k, v in data.items()}


def write_manifest(path: str | Path, mapping: dict[str, str]) -> None:
    """Write the manifest atomically (sorted keys, UTF-8)."""
    payload = json.dumps(mapping, indent=2, ensure_ascii=False, sort_keys=True) + "\n"
    atomic_write(path, payload.encode("utf-8"))
