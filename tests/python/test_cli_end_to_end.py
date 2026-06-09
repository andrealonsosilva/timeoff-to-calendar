"""T015/T023 — full CLI pipeline (fetch stubbed) writes a valid filtered feed."""

from pathlib import Path

from icalendar import Calendar

import filter_ics.cli as cli
from filter_ics.cli import EXIT_OK, main

FIXTURE = Path(__file__).parents[1] / "fixtures" / "source.ics"


def test_cli_success_writes_filtered_feed(tmp_path, monkeypatch, capsys):
    monkeypatch.setattr(cli, "fetch_calendar", lambda url, **kw: FIXTURE.read_bytes())
    monkeypatch.setenv("SOURCE_ICS_URL", "https://example.invalid/feed?token=SECRET")

    names = tmp_path / "names.json"
    names.write_text('["Pedro Fernandes", "Thiago Bessa"]', encoding="utf-8")
    output = tmp_path / "whos-out.ics"

    code = main(["--allowlist", str(names), "--output", str(output)])
    assert code == EXIT_OK

    cal = Calendar.from_ical(output.read_bytes())
    uids = {str(c.get("UID")) for c in cal.walk("VEVENT")}
    assert uids == {"evt-pedro-1", "evt-thiago-1"}

    out = capsys.readouterr().out
    assert "kept 2" in out
    assert "SECRET" not in out  # token never logged


def test_cli_missing_source_url_exit_2(tmp_path, monkeypatch):
    monkeypatch.delenv("SOURCE_ICS_URL", raising=False)
    names = tmp_path / "names.json"
    names.write_text("[]", encoding="utf-8")
    assert main(["--allowlist", str(names), "--output", str(tmp_path / "o.ics")]) == 2
