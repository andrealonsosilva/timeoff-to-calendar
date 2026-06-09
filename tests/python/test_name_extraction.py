"""T013 — name extraction from SUMMARY."""

import pytest

from filter_ics import extract_name


@pytest.mark.parametrize(
    "summary, expected",
    [
        ("John Doe (Folga - 11 dias)", "John Doe"),
        ("Richard Doe (Folga - 15 dias)", "Richard Doe"),
        ("Janet Doe", "Janet Doe"),  # no parenthetical
        ("  Trimmed Name  (Folga)", "Trimmed Name"),
        ("Renée Doe (Férias)", "Renée Doe"),  # accents preserved
    ],
)
def test_extract_name(summary, expected):
    assert extract_name(summary) == expected
