"""T027 — allowlist failures are safe: exit 4, output untouched (FR-012)."""

from pathlib import Path

import pytest

from filter_ics.allowlist import AllowlistError, load_allowlist
from filter_ics.cli import EXIT_ALLOWLIST, main


def test_missing_file_raises(tmp_path):
    with pytest.raises(AllowlistError):
        load_allowlist(tmp_path / "does-not-exist.json")


def test_invalid_json_raises(tmp_path):
    bad = tmp_path / "names.json"
    bad.write_text("{ not json", encoding="utf-8")
    with pytest.raises(AllowlistError):
        load_allowlist(bad)


def test_not_an_array_raises(tmp_path):
    bad = tmp_path / "names.json"
    bad.write_text('{"name": "x"}', encoding="utf-8")
    with pytest.raises(AllowlistError):
        load_allowlist(bad)


def test_empty_string_entry_raises(tmp_path):
    bad = tmp_path / "names.json"
    bad.write_text('["ok", "  "]', encoding="utf-8")
    with pytest.raises(AllowlistError):
        load_allowlist(bad)


def test_cli_bad_allowlist_exit_4_and_output_untouched(tmp_path, monkeypatch):
    output = tmp_path / "whos-out.ics"
    output.write_bytes(b"PREVIOUS GOOD FEED")
    bad = tmp_path / "names.json"
    bad.write_text("{ not json", encoding="utf-8")
    monkeypatch.setenv("SOURCE_ICS_URL", "https://example.invalid/feed")

    code = main(["--allowlist", str(bad), "--output", str(output)])

    assert code == EXIT_ALLOWLIST
    assert output.read_bytes() == b"PREVIOUS GOOD FEED"  # last-good preserved
