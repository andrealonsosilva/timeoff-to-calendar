"""Parse the source calendar and keep only allowlisted people's events.

Name rule (contracts/output-ics.md): the person name is the SUMMARY truncated at the
first ``" ("`` and trimmed; if there is no ``" ("`` the whole trimmed SUMMARY is the name.
"""

from __future__ import annotations

from dataclasses import dataclass, field

from icalendar import Calendar

from .allowlist import Allowlist


class ParseError(Exception):
    """Raised when the source body is not a valid iCalendar document (exit 3)."""


@dataclass
class FilterResult:
    calendar: Calendar
    read: int = 0
    kept: int = 0
    dropped: int = 0
    # Allowlist names (original spelling) that matched zero events — likely typos.
    unmatched_names: list[str] = field(default_factory=list)


def extract_name(summary: str) -> str:
    """Return the person name from an event SUMMARY."""
    idx = summary.find(" (")
    name = summary if idx == -1 else summary[:idx]
    return name.strip()


def filter_calendar(raw: bytes, allowlist: Allowlist) -> FilterResult:
    """Filter ``raw`` iCalendar bytes, keeping only allowlisted people's VEVENTs.

    Non-VEVENT subcomponents (e.g. VTIMEZONE) and all calendar-level properties are
    preserved unchanged. Raises :class:`ParseError` if the body cannot be parsed.
    """
    try:
        cal = Calendar.from_ical(raw)
    except Exception as exc:  # icalendar raises ValueError and other parse errors
        raise ParseError(f"source is not valid iCalendar: {exc}") from exc

    result = FilterResult(calendar=cal)
    matched: set[str] = set()
    kept_subcomponents = []

    for component in cal.subcomponents:
        if component.name == "VEVENT":
            result.read += 1
            name = extract_name(str(component.get("SUMMARY", "")))
            if allowlist.contains(name):
                result.kept += 1
                matched.add(name.strip().casefold())
                kept_subcomponents.append(component)
            else:
                result.dropped += 1
        else:
            kept_subcomponents.append(component)

    cal.subcomponents = kept_subcomponents
    result.unmatched_names = [n for n in allowlist.names if n.strip().casefold() not in matched]
    return result
