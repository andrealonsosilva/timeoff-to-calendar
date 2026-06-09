"""Serialize the filtered calendar and write it atomically."""

from __future__ import annotations

import os
import tempfile
from pathlib import Path

from icalendar import Calendar


def render(calendar: Calendar) -> bytes:
    """Serialize the calendar to iCalendar bytes."""
    return calendar.to_ical()


def atomic_write(path: str | Path, data: bytes) -> None:
    """Write ``data`` to ``path`` atomically.

    Writes to a temp file in the same directory then replaces the target, so a crash or
    error never leaves a partial/empty published feed (FR-011/FR-012).
    """
    target = Path(path)
    directory = target.parent if target.parent.parts else Path(".")
    directory.mkdir(parents=True, exist_ok=True)
    fd, tmp = tempfile.mkstemp(dir=str(directory), suffix=".tmp")
    try:
        with os.fdopen(fd, "wb") as handle:
            handle.write(data)
        os.replace(tmp, target)
    except BaseException:
        try:
            os.unlink(tmp)
        except OSError:
            pass
        raise
