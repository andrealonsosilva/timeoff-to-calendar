"""T028 — one bad file never breaks the others; last-good preserved; safe names."""

from pathlib import Path

from filter_ics import feeds

FIXTURE = Path(__file__).parents[1] / "fixtures" / "source.ics"


def _fetch(url, **kw):
    return FIXTURE.read_bytes()


def _write(dir_path: Path, name: str, body: str) -> None:
    (dir_path / name).write_text(body, encoding="utf-8")


def test_invalid_file_skipped_others_published(tmp_path):
    allow = tmp_path / "allowlists"
    allow.mkdir()
    _write(allow, "whos-out.json", '{"fileName": "whos-out", "names": ["John Doe"]}')
    _write(allow, "engineering.json", '{"fileName": "engineering", "names": ["Jane Doe"]}')
    _write(allow, "broken.json", "{ not json")
    out = tmp_path / "public"

    result = feeds.run("u", allow, out, fetcher=_fetch)

    assert (out / "whos-out.ics").exists()
    assert (out / "engineering.ics").exists()
    assert any(b == "broken.json" for b, _ in result.skipped)
    assert result.total == 3


def test_errored_file_preserves_last_good(tmp_path):
    allow = tmp_path / "allowlists"
    allow.mkdir()
    _write(allow, "engineering.json", '{"fileName": "engineering", "names": ["John Doe"]}')
    out = tmp_path / "public"
    feeds.run("u", allow, out, fetcher=_fetch)
    good = (out / "engineering.ics").read_bytes()

    # Corrupt the file and rerun — the published feed must be byte-unchanged (G1).
    _write(allow, "engineering.json", "{ broken")
    result = feeds.run("u", allow, out, fetcher=_fetch)

    assert (out / "engineering.ics").read_bytes() == good
    assert any(b == "engineering.json" for b, _ in result.skipped)
    assert "engineering" not in result.removed


def test_duplicate_output_names_both_skipped(tmp_path):
    allow = tmp_path / "allowlists"
    allow.mkdir()
    _write(allow, "a.json", '{"fileName": "team", "names": ["John Doe"]}')
    _write(allow, "b.json", '{"fileName": "team", "names": ["Jane Doe"]}')
    out = tmp_path / "public"

    result = feeds.run("u", allow, out, fetcher=_fetch)

    assert not (out / "team.ics").exists()
    assert {b for b, _ in result.skipped} == {"a.json", "b.json"}


def test_path_traversal_rejected(tmp_path):
    allow = tmp_path / "allowlists"
    allow.mkdir()
    _write(allow, "evil.json", '{"fileName": "../evil", "names": ["John Doe"]}')
    out = tmp_path / "public"

    result = feeds.run("u", allow, out, fetcher=_fetch)

    assert any(b == "evil.json" for b, _ in result.skipped)
    assert not (tmp_path / "evil.ics").exists()  # nothing escaped output_dir
