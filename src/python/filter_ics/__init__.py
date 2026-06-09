"""Filter a time-off iCalendar feed into one or more allowlist-defined .ics files."""

from .allowlist import (
    Allowlist,
    FeedError,
    build_allowlist,
    load_feed_file,
    parse_feed_object,
)
from .feeds import FeedDefinition, RunResult, load_feeds, run
from .fetch import FetchError, fetch_calendar
from .filter import FilterResult, ParseError, extract_name, filter_calendar
from .manifest import read_manifest, write_manifest
from .naming import output_filename, sanitize_output_name
from .render import atomic_write, render

__all__ = [
    "Allowlist",
    "FeedError",
    "build_allowlist",
    "load_feed_file",
    "parse_feed_object",
    "FeedDefinition",
    "RunResult",
    "load_feeds",
    "run",
    "FetchError",
    "fetch_calendar",
    "FilterResult",
    "ParseError",
    "extract_name",
    "filter_calendar",
    "read_manifest",
    "write_manifest",
    "output_filename",
    "sanitize_output_name",
    "atomic_write",
    "render",
]
