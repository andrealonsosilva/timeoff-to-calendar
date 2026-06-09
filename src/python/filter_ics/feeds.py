"""Multi-feed orchestration: fetch once, filter per file, reconcile published outputs.

See contracts/cli.md and contracts/publishing.md.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from pathlib import Path

from .allowlist import Allowlist, FeedError, build_allowlist, load_feed_file
from .fetch import fetch_calendar
from .filter import filter_calendar
from .manifest import read_manifest, write_manifest
from .naming import output_filename, sanitize_output_name
from .render import atomic_write, render


@dataclass(frozen=True)
class FeedDefinition:
    basename: str  # e.g. "engineering.json" — manifest key
    output_name: str  # sanitized base name, no extension
    output_file: str  # "<output_name>.ics"
    allowlist: Allowlist


@dataclass
class RunResult:
    total: int = 0  # allowlist files discovered
    written: list[str] = field(default_factory=list)  # output names written/updated
    skipped: list[tuple[str, str]] = field(default_factory=list)  # (basename, reason)
    removed: list[str] = field(default_factory=list)  # output names deleted
    source_bytes: int = 0
    unmatched: dict[str, list[str]] = field(default_factory=dict)  # output_name -> names


def load_feeds(allowlists_dir: str | Path) -> tuple[list[FeedDefinition], list[tuple[str, str]], list[str]]:
    """Parse every ``*.json`` in the folder. Returns (valid feeds, errors, discovered basenames)."""
    directory = Path(allowlists_dir)
    feeds: list[FeedDefinition] = []
    errors: list[tuple[str, str]] = []
    discovered: list[str] = []
    for path in sorted(directory.glob("*.json")):
        discovered.append(path.name)
        try:
            file_name, names = load_feed_file(path)
            output_name = sanitize_output_name(file_name)
            allowlist = build_allowlist(names)
        except FeedError as exc:
            errors.append((path.name, str(exc)))
            continue
        feeds.append(FeedDefinition(path.name, output_name, output_filename(output_name), allowlist))
    return feeds, errors, discovered


def run(
    source_url: str,
    allowlists_dir: str | Path,
    output_dir: str | Path,
    *,
    fetcher=fetch_calendar,
) -> RunResult:
    """Produce/refresh all feeds from one source fetch and reconcile the output directory.

    Raises FetchError (source fetch) or ParseError (source not iCalendar) — both map to a
    whole-run failure that writes nothing. Per-feed problems are recorded in the result and
    never abort the others.
    """
    feeds, errors, discovered = load_feeds(allowlists_dir)

    # Duplicate output names → every colliding file is an error (FR-007).
    by_output: dict[str, list[FeedDefinition]] = {}
    for feed in feeds:
        by_output.setdefault(feed.output_file, []).append(feed)
    valid: list[FeedDefinition] = []
    for output_file, group in by_output.items():
        if len(group) > 1:
            for feed in group:
                errors.append((feed.basename, f"duplicate output name '{output_file}'"))
        else:
            valid.append(group[0])

    # Fetch ONCE for all feeds; parse-check once (raises ParseError on invalid source).
    raw = fetcher(source_url)
    filter_calendar(raw, build_allowlist([]))  # validates the source parses

    out_dir = Path(output_dir)
    out_dir.mkdir(parents=True, exist_ok=True)
    manifest_path = out_dir / ".feeds.json"
    old = read_manifest(manifest_path)
    new: dict[str, str] = {}
    result = RunResult(total=len(discovered), source_bytes=len(raw))

    for feed in valid:
        filtered = filter_calendar(raw, feed.allowlist)
        atomic_write(out_dir / feed.output_file, render(filtered.calendar))
        result.written.append(feed.output_name)
        new[feed.basename] = feed.output_name
        if filtered.unmatched_names:
            result.unmatched[feed.output_name] = filtered.unmatched_names

    # Errored files: record; carry over last-good (keep manifest entry, leave file untouched).
    for basename, reason in errors:
        result.skipped.append((basename, reason))
        if basename in old and basename not in new:
            new[basename] = old[basename]

    # Reconcile removals/renames: any previously published output not produced this run is deleted.
    produced = set(new.values())
    for old_name in sorted(set(old.values())):
        if old_name not in produced:
            target = out_dir / output_filename(old_name)
            if target.exists():
                target.unlink()
            result.removed.append(old_name)

    write_manifest(manifest_path, new)
    return result
