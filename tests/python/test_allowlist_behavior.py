"""T029 — allowlist load behavior: dedupe, empty, normalization."""

from filter_ics.allowlist import load_allowlist


def test_loads_names(tmp_path):
    f = tmp_path / "names.json"
    f.write_text('["Pedro Fernandes", "Thiago Bessa"]', encoding="utf-8")
    al = load_allowlist(f)
    assert al.names == ("Pedro Fernandes", "Thiago Bessa")
    assert al.contains("pedro fernandes")
    assert not al.contains("Maria Silva")


def test_empty_array_is_valid(tmp_path):
    f = tmp_path / "names.json"
    f.write_text("[]", encoding="utf-8")
    al = load_allowlist(f)
    assert al.names == ()
    assert al.normalized == frozenset()


def test_duplicates_are_deduped(tmp_path):
    f = tmp_path / "names.json"
    f.write_text('["Pedro Fernandes", "  pedro fernandes  "]', encoding="utf-8")
    al = load_allowlist(f)
    assert al.names == ("Pedro Fernandes",)
