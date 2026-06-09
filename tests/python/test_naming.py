"""T011 — output-name sanitization."""

import pytest

from filter_ics import output_filename, sanitize_output_name
from filter_ics.allowlist import FeedError


@pytest.mark.parametrize(
    "raw, expected",
    [
        ("whos-out", "whos-out"),
        ("engineering", "engineering"),
        ("team.ics", "team"),  # trailing .ics stripped
        ("  spaced  ", "spaced"),
        ("team_v2", "team_v2"),
    ],
)
def test_sanitize_ok(raw, expected):
    assert sanitize_output_name(raw) == expected


@pytest.mark.parametrize("raw", ["", "   ", "../evil", "a/b", "a\\b", "..", ".", ".hidden", "bad name"])
def test_sanitize_rejects_unsafe(raw):
    with pytest.raises(FeedError):
        sanitize_output_name(raw)


def test_output_filename():
    assert output_filename("whos-out") == "whos-out.ics"
