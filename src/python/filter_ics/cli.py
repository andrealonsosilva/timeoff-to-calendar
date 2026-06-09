"""Command-line entry point. See contracts/cli.md.

Exit codes: 0 ok, 1 fetch error, 2 config error, 3 parse error, 4 allowlist error.
The output file is written only after a fully successful fetch + parse + filter.
"""

from __future__ import annotations

import argparse
import os
import sys
from urllib.parse import urlsplit

from .allowlist import AllowlistError, load_allowlist
from .fetch import FetchError, fetch_calendar
from .filter import ParseError, filter_calendar
from .render import atomic_write, render

EXIT_OK = 0
EXIT_FETCH = 1
EXIT_CONFIG = 2
EXIT_PARSE = 3
EXIT_ALLOWLIST = 4


def _redact(url: str) -> str:
    """Return scheme+host only, so the feed token is never logged."""
    try:
        parts = urlsplit(url)
        return f"{parts.scheme}://{parts.netloc}" if parts.scheme else "(url)"
    except ValueError:
        return "(url)"


def _build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="filter-ics",
        description="Filter a time-off .ics feed down to an allowlist of people.",
    )
    parser.add_argument("--source-url", default=None, help="Source feed URL (or env SOURCE_ICS_URL)")
    parser.add_argument("--allowlist", default="names.json", help="Path to allowlist JSON")
    parser.add_argument("--output", default="whos-out.ics", help="Output .ics path")
    parser.add_argument("--verbose", action="store_true", help="Log each kept/dropped name")
    return parser


def main(argv: list[str] | None = None) -> int:
    args = _build_parser().parse_args(argv)

    source_url = args.source_url or os.environ.get("SOURCE_ICS_URL")
    if not source_url:
        print("error: config: missing source URL (--source-url or SOURCE_ICS_URL)", file=sys.stderr)
        return EXIT_CONFIG

    # 1. Allowlist (load before fetching so a bad list fails fast, output untouched).
    try:
        allowlist = load_allowlist(args.allowlist)
    except AllowlistError as exc:
        print(f"error: allowlist: {exc}", file=sys.stderr)
        return EXIT_ALLOWLIST

    # 2. Fetch.
    try:
        raw = fetch_calendar(source_url)
    except FetchError as exc:
        print(f"error: fetch: {exc} ({_redact(source_url)})", file=sys.stderr)
        return EXIT_FETCH

    # 3. Parse + filter.
    try:
        result = filter_calendar(raw, allowlist)
    except ParseError as exc:
        print(f"error: parse: {exc}", file=sys.stderr)
        return EXIT_PARSE

    # 4. Render + atomic write (only reached on full success).
    atomic_write(args.output, render(result.calendar))

    print(
        f"ok: fetched {len(raw)}B, read {result.read} events, "
        f"kept {result.kept}, dropped {result.dropped}, allowlist {len(allowlist.names)} names"
    )
    if result.unmatched_names:
        print(f"warn: allowlist names with no matching events: {result.unmatched_names}")
    if args.verbose:
        print(f"verbose: source={_redact(source_url)} output={args.output}")

    return EXIT_OK


def run() -> None:
    """Console-script wrapper."""
    raise SystemExit(main())


if __name__ == "__main__":
    run()
