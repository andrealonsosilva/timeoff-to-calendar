"""T013 — object-form allowlist file parsing + Allowlist behavior."""

import pytest

from filter_ics import build_allowlist, parse_feed_object
from filter_ics.allowlist import FeedError


def test_parse_valid_object():
    name, names = parse_feed_object('{"fileName": "engineering", "names": ["John Doe", "Jane Doe"]}')
    assert name == "engineering"
    assert names == ["John Doe", "Jane Doe"]


@pytest.mark.parametrize(
    "raw",
    [
        "{ not json",
        "[]",  # array, not object
        '{"names": ["x"]}',  # missing fileName
        '{"fileName": "", "names": []}',  # empty fileName
        '{"fileName": "x"}',  # missing names
        '{"fileName": "x", "names": {}}',  # names not array
    ],
)
def test_parse_invalid_object_raises(raw):
    with pytest.raises(FeedError):
        parse_feed_object(raw)


def test_build_allowlist_dedupes_and_normalizes():
    al = build_allowlist(["John Doe", "  john doe  "])
    assert al.names == ("John Doe",)
    assert al.contains("JOHN DOE")
    assert not al.contains("Jane Doe")


def test_build_allowlist_empty_ok():
    al = build_allowlist([])
    assert al.names == ()


def test_build_allowlist_rejects_blank():
    with pytest.raises(FeedError):
        build_allowlist(["ok", "  "])
