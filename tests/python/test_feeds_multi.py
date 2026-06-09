"""T015 — multi-feed orchestration: one feed per file, source fetched once."""

from pathlib import Path

from icalendar import Calendar

from filter_ics import feeds

FIXTURE = Path(__file__).parents[1] / "fixtures" / "source.ics"


def _write(dir_path: Path, name: str, body: str) -> None:
    (dir_path / name).write_text(body, encoding="utf-8")


def _uids(ics_path: Path) -> set[str]:
    cal = Calendar.from_ical(ics_path.read_bytes())
    return {str(c.get("UID")) for c in cal.walk("VEVENT")}


def _counting_fetcher():
    state = {"calls": 0}

    def fetch(url, **kw):
        state["calls"] += 1
        return FIXTURE.read_bytes()

    return fetch, state


def test_one_feed_per_file_fetch_once(tmp_path):
    allow = tmp_path / "allowlists"
    allow.mkdir()
    _write(allow, "whos-out.json", '{"fileName": "whos-out", "names": ["John Doe", "Jane Doe"]}')
    _write(allow, "engineering.json", '{"fileName": "engineering", "names": ["John Doe"]}')
    out = tmp_path / "public"

    fetch, state = _counting_fetcher()
    result = feeds.run("https://example.invalid/feed", allow, out, fetcher=fetch)

    assert state["calls"] == 1  # SC-006: source fetched once
    assert set(result.written) == {"whos-out", "engineering"}
    assert _uids(out / "whos-out.ics") == {"evt-john-1", "evt-jane-1"}
    assert _uids(out / "engineering.ics") == {"evt-john-1"}

    # Manifest records both feeds.
    import json

    manifest = json.loads((out / ".feeds.json").read_text(encoding="utf-8"))
    assert manifest == {"engineering.json": "engineering", "whos-out.json": "whos-out"}


def test_filename_with_ics_extension_not_doubled(tmp_path):
    allow = tmp_path / "allowlists"
    allow.mkdir()
    _write(allow, "team.json", '{"fileName": "team.ics", "names": ["John Doe"]}')
    out = tmp_path / "public"
    fetch, _ = _counting_fetcher()

    feeds.run("https://example.invalid/feed", allow, out, fetcher=fetch)
    assert (out / "team.ics").exists()
    assert not (out / "team.ics.ics").exists()
