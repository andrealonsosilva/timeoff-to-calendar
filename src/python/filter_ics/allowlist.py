"""Allowlist primitives and per-feed allowlist-file parsing.

A feed is one JSON object file: ``{ "fileName": "...", "names": [...] }`` — see
``specs/002-multi-ics-feeds/contracts/allowlist-file.schema.json``. Matching is
case-insensitive and whitespace-trimmed (unchanged from feature 001).
"""

from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path


class FeedError(Exception):
    """A per-feed problem (bad JSON, missing fileName, bad names). The feed is skipped."""


def _normalize(name: str) -> str:
    return name.strip().casefold()


@dataclass(frozen=True)
class Allowlist:
    """An immutable allowlist used for matching."""

    names: tuple[str, ...]
    normalized: frozenset[str]

    def contains(self, name: str) -> bool:
        return _normalize(name) in self.normalized


def build_allowlist(names: list) -> Allowlist:
    """Build an :class:`Allowlist` from a list of name strings (de-duped, trimmed).

    Raises :class:`FeedError` if any entry is not a non-empty string.
    """
    seen: set[str] = set()
    ordered: list[str] = []
    for item in names:
        if not isinstance(item, str) or not item.strip():
            raise FeedError("'names' entries must be non-empty strings")
        trimmed = item.strip()
        key = _normalize(trimmed)
        if key not in seen:
            seen.add(key)
            ordered.append(trimmed)
    return Allowlist(names=tuple(ordered), normalized=frozenset(seen))


def parse_feed_object(raw: str) -> tuple[str, list]:
    """Parse an allowlist-file body into ``(fileName, names)``. Raises :class:`FeedError`."""
    try:
        data = json.loads(raw)
    except json.JSONDecodeError as exc:
        raise FeedError(f"not valid JSON: {exc}") from exc

    if not isinstance(data, dict):
        raise FeedError("allowlist file must be a JSON object")

    file_name = data.get("fileName")
    if not isinstance(file_name, str) or not file_name.strip():
        raise FeedError("missing or empty 'fileName'")

    names = data.get("names")
    if not isinstance(names, list):
        raise FeedError("'names' must be an array")

    return file_name, names


def load_feed_file(path: str | Path) -> tuple[str, list]:
    """Read and parse one allowlist file into ``(fileName, names)``. Raises :class:`FeedError`."""
    p = Path(path)
    try:
        raw = p.read_text(encoding="utf-8")
    except FileNotFoundError as exc:
        raise FeedError(f"file not found: {p}") from exc
    except OSError as exc:
        raise FeedError(f"cannot read {p}: {exc}") from exc
    return parse_feed_object(raw)
