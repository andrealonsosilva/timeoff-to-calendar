"""T015 — fixture filtering: invariants I1-I3 from contracts/output-ics.md."""

from pathlib import Path

from icalendar import Calendar

from filter_ics import Allowlist, filter_calendar
from filter_ics.allowlist import _normalize

FIXTURE = Path(__file__).parents[1] / "fixtures" / "source.ics"


def _allowlist(*names: str) -> Allowlist:
    norm = frozenset(_normalize(n) for n in names)
    return Allowlist(names=tuple(names), normalized=norm)


def _uids(cal: Calendar) -> set[str]:
    return {str(c.get("UID")) for c in cal.walk("VEVENT")}


def test_keeps_only_allowlisted_people():
    raw = FIXTURE.read_bytes()
    result = filter_calendar(raw, _allowlist("Pedro Fernandes", "Thiago Bessa"))

    assert result.read == 4
    assert result.kept == 2
    assert result.dropped == 2
    # I3: count matches; I1/I2: exactly the allowlisted people's events remain.
    assert _uids(result.calendar) == {"evt-pedro-1", "evt-thiago-1"}


def test_case_and_whitespace_insensitive():
    raw = FIXTURE.read_bytes()
    result = filter_calendar(raw, _allowlist("  pedro fernandes  "))
    assert _uids(result.calendar) == {"evt-pedro-1"}


def test_empty_allowlist_yields_no_events():
    raw = FIXTURE.read_bytes()
    result = filter_calendar(raw, _allowlist())
    assert result.kept == 0
    assert _uids(result.calendar) == set()


def test_calendar_properties_preserved():
    raw = FIXTURE.read_bytes()
    result = filter_calendar(raw, _allowlist("Pedro Fernandes"))
    assert str(result.calendar.get("X-WR-CALNAME")) == "Quem está fora"
    assert str(result.calendar.get("VERSION")) == "2.0"


def test_kept_event_preserved_verbatim():
    raw = FIXTURE.read_bytes()
    result = filter_calendar(raw, _allowlist("Pedro Fernandes"))
    (event,) = list(result.calendar.walk("VEVENT"))
    assert str(event.get("SUMMARY")) == "Pedro Fernandes (Folga - 11 dias)"
    assert str(event.get("DESCRIPTION")) == "Folga (mai 18 – jun 1)"
    assert event.get("DTSTART").to_ical() == b"20260518"


def test_unmatched_names_reported():
    raw = FIXTURE.read_bytes()
    result = filter_calendar(raw, _allowlist("Pedro Fernandes", "Nobody Here"))
    assert result.unmatched_names == ["Nobody Here"]
