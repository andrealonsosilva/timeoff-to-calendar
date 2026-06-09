"""T023 — folder curation: removing a file removes its feed; adding adds one."""

from pathlib import Path

from filter_ics import feeds

FIXTURE = Path(__file__).parents[1] / "fixtures" / "source.ics"


def _fetch(url, **kw):
    return FIXTURE.read_bytes()


def _write(dir_path: Path, name: str, body: str) -> None:
    (dir_path / name).write_text(body, encoding="utf-8")


def test_remove_file_removes_feed(tmp_path):
    allow = tmp_path / "allowlists"
    allow.mkdir()
    _write(allow, "whos-out.json", '{"fileName": "whos-out", "names": ["John Doe"]}')
    _write(allow, "engineering.json", '{"fileName": "engineering", "names": ["John Doe"]}')
    out = tmp_path / "public"

    feeds.run("u", allow, out, fetcher=_fetch)
    assert (out / "engineering.ics").exists()

    # Remove one allowlist file and rerun.
    (allow / "engineering.json").unlink()
    result = feeds.run("u", allow, out, fetcher=_fetch)

    assert not (out / "engineering.ics").exists()  # G2: removed
    assert (out / "whos-out.ics").exists()  # untouched
    assert "engineering" in result.removed

    import json

    manifest = json.loads((out / ".feeds.json").read_text(encoding="utf-8"))
    assert manifest == {"whos-out.json": "whos-out"}


def test_add_file_adds_feed(tmp_path):
    allow = tmp_path / "allowlists"
    allow.mkdir()
    _write(allow, "whos-out.json", '{"fileName": "whos-out", "names": ["John Doe"]}')
    out = tmp_path / "public"
    feeds.run("u", allow, out, fetcher=_fetch)

    _write(allow, "engineering.json", '{"fileName": "engineering", "names": ["Jane Doe"]}')
    result = feeds.run("u", allow, out, fetcher=_fetch)

    assert (out / "engineering.ics").exists()
    assert "engineering" in result.written
