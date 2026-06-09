"""Command-line entry point (multi-feed). See contracts/cli.md.

Exit codes: 0 ok (deployable; per-feed failures only logged), 1 source fetch/parse failed,
2 config error. The output dir is only mutated for feeds that succeed or are removed.
"""

from __future__ import annotations

import argparse
import os
import sys
from pathlib import Path
from urllib.parse import urlsplit

from . import feeds as feeds_module
from .fetch import FetchError
from .filter import ParseError

EXIT_OK = 0
EXIT_FETCH = 1
EXIT_CONFIG = 2


def _redact(url: str) -> str:
    try:
        parts = urlsplit(url)
        return f"{parts.scheme}://{parts.netloc}" if parts.scheme else "(url)"
    except ValueError:
        return "(url)"


def _build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="filter-ics",
        description="Filter a time-off feed into multiple .ics files, one per allowlist file.",
    )
    parser.add_argument("--source-url", default=None, help="Source feed URL (or env SOURCE_ICS_URL)")
    parser.add_argument("--allowlists-dir", default="allowlists", help="Folder of allowlist JSON files")
    parser.add_argument("--output-dir", default="public", help="Directory to publish .ics files into")
    parser.add_argument("--verbose", action="store_true", help="Log per-feed details")
    return parser


def main(argv: list[str] | None = None) -> int:
    args = _build_parser().parse_args(argv)

    source_url = args.source_url or os.environ.get("SOURCE_ICS_URL")
    if not source_url:
        print("error: config: missing source URL (--source-url or SOURCE_ICS_URL)", file=sys.stderr)
        return EXIT_CONFIG

    if not Path(args.allowlists_dir).is_dir():
        print(f"error: config: allowlists dir not found: {args.allowlists_dir}", file=sys.stderr)
        return EXIT_CONFIG

    try:
        result = feeds_module.run(source_url, args.allowlists_dir, args.output_dir)
    except FetchError as exc:
        print(f"error: fetch: {exc} ({_redact(source_url)})", file=sys.stderr)
        return EXIT_FETCH
    except ParseError as exc:
        print(f"error: source parse: {exc}", file=sys.stderr)
        return EXIT_FETCH

    for basename, reason in result.skipped:
        print(f"error: feed {basename}: {reason}", file=sys.stderr)
    for output_name, names in result.unmatched.items():
        print(f"warn: {output_name}: allowlist names with no matching events: {names}")

    print(
        f"summary: feeds total={result.total} written={len(result.written)} "
        f"skipped={len(result.skipped)} removed={len(result.removed)}, "
        f"source {result.source_bytes}B"
    )
    if args.verbose:
        print(f"verbose: source={_redact(source_url)} output_dir={args.output_dir} "
              f"written={result.written} removed={result.removed}")

    return EXIT_OK


def run() -> None:
    raise SystemExit(main())


if __name__ == "__main__":
    run()
