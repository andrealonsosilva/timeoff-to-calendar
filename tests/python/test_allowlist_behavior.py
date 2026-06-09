"""T029 — allowlist load behavior: dedupe, empty, normalization."""

from filter_ics.allowlist import load_allowlist


def test_loads_names(tmp_path):
    f = tmp_path / "names.json"
    f.write_text('["John Doe", "Jane Doe"]', encoding="utf-8")
    al = load_allowlist(f)
    assert al.names == ("John Doe", "Jane Doe")
    assert al.contains("john doe")
    assert not al.contains("Janet Doe")


def test_empty_array_is_valid(tmp_path):
    f = tmp_path / "names.json"
    f.write_text("[]", encoding="utf-8")
    al = load_allowlist(f)
    assert al.names == ()
    assert al.normalized == frozenset()


def test_duplicates_are_deduped(tmp_path):
    f = tmp_path / "names.json"
    f.write_text('["John Doe", "  john doe  "]', encoding="utf-8")
    al = load_allowlist(f)
    assert al.names == ("John Doe",)
