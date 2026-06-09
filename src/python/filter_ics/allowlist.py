"""Load and validate the people allowlist (``names.json``).

Contract: ``specs/001-filter-ics-people/contracts/allowlist.schema.json`` — a flat JSON
array of non-empty name strings. Matching is case-insensitive and whitespace-trimmed.
"""

from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path


class AllowlistError(Exception):
    """Raised when the allowlist file is missing, not JSON, or schema-invalid (exit 4)."""


def _normalize(name: str) -> str:
    return name.strip().casefold()


@dataclass(frozen=True)
class Allowlist:
    """An immutable allowlist.

    ``names`` keeps the original (trimmed) spellings for reporting; ``normalized`` is the
    lookup set used for matching.
    """

    names: tuple[str, ...]
    normalized: frozenset[str]

    def contains(self, name: str) -> bool:
        return _normalize(name) in self.normalized


def load_allowlist(path: str | Path) -> Allowlist:
    """Load ``names.json`` into an :class:`Allowlist`.

    Raises :class:`AllowlistError` on any problem so the caller can exit 4 without
    touching the published output.
    """
    p = Path(path)
    try:
        raw = p.read_text(encoding="utf-8")
    except FileNotFoundError as exc:
        raise AllowlistError(f"allowlist file not found: {p}") from exc
    except OSError as exc:
        raise AllowlistError(f"cannot read allowlist file {p}: {exc}") from exc

    try:
        data = json.loads(raw)
    except json.JSONDecodeError as exc:
        raise AllowlistError(f"allowlist is not valid JSON: {exc}") from exc

    if not isinstance(data, list):
        raise AllowlistError("allowlist must be a JSON array of name strings")

    seen: set[str] = set()
    names: list[str] = []
    for item in data:
        if not isinstance(item, str) or not item.strip():
            raise AllowlistError("allowlist entries must be non-empty strings")
        trimmed = item.strip()
        key = _normalize(trimmed)
        if key not in seen:
            seen.add(key)
            names.append(trimmed)

    return Allowlist(names=tuple(names), normalized=frozenset(seen))
