"""Fetch the source iCalendar feed from a URL."""

from __future__ import annotations

import httpx


class FetchError(Exception):
    """Raised on network error, timeout, or non-2xx response (exit 1)."""


def fetch_calendar(url: str, timeout: float = 30.0) -> bytes:
    """Return the raw body of the source feed.

    Raises :class:`FetchError` on any transport error or non-2xx status.
    """
    try:
        response = httpx.get(url, timeout=timeout, follow_redirects=True)
    except httpx.HTTPError as exc:
        raise FetchError(f"could not fetch source feed: {exc}") from exc

    if response.status_code // 100 != 2:
        raise FetchError(f"source feed returned HTTP {response.status_code}")

    return response.content
