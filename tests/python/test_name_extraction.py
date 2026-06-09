"""T013 — name extraction from SUMMARY."""

import pytest

from filter_ics import extract_name


@pytest.mark.parametrize(
    "summary, expected",
    [
        ("Pedro Fernandes (Folga - 11 dias)", "Pedro Fernandes"),
        ("Luciano Lizzoni (Folga - 15 dias)", "Luciano Lizzoni"),
        ("Maria Silva", "Maria Silva"),  # no parenthetical
        ("  Trimmed Name  (Folga)", "Trimmed Name"),
        ("José da Conceição (Férias)", "José da Conceição"),  # accents preserved
    ],
)
def test_extract_name(summary, expected):
    assert extract_name(summary) == expected
