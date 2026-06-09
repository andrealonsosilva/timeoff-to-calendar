"""Output-name sanitization for feed files (see contracts/publishing.md G4)."""

from __future__ import annotations

import re

from .allowlist import FeedError

# A safe single path segment: letters, digits, dot, dash, underscore.
_SAFE = re.compile(r"^[A-Za-z0-9._-]+$")


def sanitize_output_name(file_name: str) -> str:
    """Return a safe base name (no extension) for the feed's `.ics`.

    Strips a trailing ``.ics`` (case-insensitive) once, then validates the result is a
    single safe path segment — no separators, no ``.``/``..``, no leading dot. Raises
    :class:`FeedError` for empty/unsafe values (prevents path traversal).
    """
    name = file_name.strip()
    if name.lower().endswith(".ics"):
        name = name[:-4]
    if (
        not name
        or name in (".", "..")
        or "/" in name
        or "\\" in name
        or name.startswith(".")
        or not _SAFE.match(name)
    ):
        raise FeedError(f"unsafe or empty fileName: {file_name!r}")
    return name


def output_filename(output_name: str) -> str:
    """The published file name for a sanitized output name."""
    return f"{output_name}.ics"
