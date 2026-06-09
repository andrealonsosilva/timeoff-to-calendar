# Implementation Plan: Multiple Filtered Calendar Feeds

**Branch**: `002-multi-ics-feeds` | **Date**: 2026-06-09 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-multi-ics-feeds/spec.md`

## Summary

Extend the single-feed tool (feature `001-filter-ics-people`) to produce **many** filtered
`.ics` files from **many** allowlist files kept in a folder. Each allowlist file becomes an
object `{ "fileName": "...", "names": [...] }`; the source calendar is fetched once and filtered
once per file, and each result is published at its own stable GitHub Pages URL
(`…/<fileName>.ics`). One bad allowlist file must not break the others, and no previously
published feed may be emptied or removed because of a failure.

Continues the **two-implementation** approach (Python + C#). Both are extended in lock-step and
remain behavior-equivalent on a shared fixture set.

### Resolved clarifications (recommended defaults — confirm or override)

- **Q1 (keys)**: each allowlist file is `{ "fileName": "<base name>", "names": ["John Doe", …] }`.
  Chosen over the literal spaced key `"file name"` for ease of editing. **Override is cheap** —
  it only changes the parsed key string + schema.
- **Q2 (legacy)**: full switch to the folder model; the legacy flat-array `names.json` is no
  longer read. The current real `names.json` is migrated to `allowlists/whos-out.json` with
  `fileName: "whos-out"` so the **existing Outlook subscription URL keeps working**.

## Technical Context

**Language/Version**: Python 3.12+ **and** C# / .NET 8+ (installed: 3.14, .NET 10) — both implementations, as in 001.

**Primary Dependencies**: unchanged — Python `icalendar` + `httpx` (pytest); C# `Ical.Net` + `HttpClient` (xUnit).

**Storage**: Files only — `allowlists/*.json` (feed definitions, committed), `public/*.ics`
(published feeds, **committed** so last-good persists across runs), `public/.feeds.json`
(manifest mapping source file → output name, for removal detection). No database.

**Testing**: `pytest` + `xUnit`; shared fixtures extended with a multi-file allowlist folder and expected per-feed outputs.

**Target Platform**: GitHub Actions `ubuntu-latest`; feeds served from GitHub Pages. Runs locally too.

**Project Type**: Command-line tool, two language implementations, one repo.

**Performance Goals**: Trivial — one source fetch per run; filtering N files over a small calendar completes in seconds. N expected in the low tens.

**Constraints**:
- Source fetched **once** per run regardless of feed count (FR-006, SC-006).
- Per-feed last-good: an invalid/removed-from-build feed must not empty or delete an
  already-published feed (FR-008, SC-004). Achieved via a committed `public/` + manifest.
- Output names sanitized; no path traversal; duplicate output names detected (FR-007).
- Each feed URL stays constant across refreshes (FR-005).

**Scale/Scope**: One source feed → N output feeds (low tens). Twice-daily refresh.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The constitution (`.specify/memory/constitution.md`) is still the unfilled template — no
ratified gates. **Status: PASS (vacuously)**, pre- and post-design. The design stays simple: a
folder iteration around the existing single-feed pipeline plus a small manifest; no new
services or external systems. Complexity Tracking left empty.

## Project Structure

### Documentation (this feature)

```text
specs/002-multi-ics-feeds/
├── plan.md, research.md, data-model.md, quickstart.md
└── contracts/
    ├── cli.md                  # updated multi-feed CLI
    ├── allowlist-file.schema.json  # the { fileName, names } object schema
    └── publishing.md           # committed public/ + manifest + last-good/removal rules
```

### Source Code (repository root)

```text
allowlists/                     # NEW: one JSON object per feed
├── whos-out.json               # migrated from the old names.json (fileName: "whos-out")
└── <group>.json                # additional feeds

public/                         # NEW: committed published feeds (Pages serves this)
├── <fileName>.ics              # one per feed
└── .feeds.json                 # manifest: source-file → fileName (removal detection)

src/python/filter_ics/
├── allowlist.py                # CHANGED: parse { fileName, names } object; load a folder → [FeedDef]
├── feeds.py                    # NEW: orchestrate fetch-once → per-feed filter/render/write; isolation
├── naming.py                   # NEW: sanitize fileName, ensure single .ics, detect duplicates
├── manifest.py                 # NEW: read/write public/.feeds.json; compute removed feeds
├── fetch.py / filter.py / render.py / cli.py   # CHANGED: cli switches to dir-based args
src/dotnet/FilterIcs/
├── Allowlist.cs (CHANGED), Feeds.cs (NEW), Naming.cs (NEW), Manifest.cs (NEW),
└── Fetch.cs / Filter.cs / Render.cs / Program.cs (CHANGED)

tests/fixtures/allowlists/      # NEW: sample multi-file folder + expected outputs
tests/python/ , tests/dotnet/   # extended

.github/workflows/publish.yml   # CHANGED: run tool over the folder, commit refreshed public/, deploy
.gitignore                      # CHANGED: stop ignoring published .ics (public/ is committed now)
```

**Structure Decision**: Keep the single repo + two implementations. Add a small orchestration
layer (`feeds` + `naming` + `manifest`) around the unchanged per-feed pipeline (fetch/filter/
render). The published `public/` directory becomes **committed state** so each feed's last-good
survives a run in which its file is invalid (the run only overwrites feeds it successfully
regenerates, carries over errored ones, and deletes only feeds whose source file was removed).

## Complexity Tracking

> No constitution gates defined; nothing to justify. The one notable choice — committing the
> generated `public/` directory — is justified in research R2 (it is what makes per-feed
> last-good preservation possible under GitHub Pages).

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| (none)    | —          | —                                   |
