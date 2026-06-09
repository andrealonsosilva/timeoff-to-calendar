# Implementation Plan: Filtered Time-Off Calendar Feed

**Branch**: `001-filter-ics-people` | **Date**: 2026-06-09 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-filter-ics-people/spec.md`

## Summary

Fetch the upstream "Who's out" iCalendar feed from a configured URL, keep only the
events whose person (the name at the start of each event title) appears in a JSON
allowlist, and publish the filtered `.ics` at a stable GitHub Pages URL that Outlook
subscribes to. A scheduled GitHub Actions workflow runs the filter twice a day. On any
fetch/parse/allowlist failure the previously published feed is left untouched.

Per the user's direction, the tool is planned in **two parallel reference
implementations — Python and C# / .NET** — that produce equivalent output, share the
same configuration (`names.json`, source URL), and obey the same CLI/output contracts.
The GitHub Actions workflow runs one implementation (selectable); the other is kept in
sync as an alternative.

## Technical Context

**Language/Version**: Python 3.12 **and** C# / .NET 8 (both implementations maintained in parallel)

**Primary Dependencies**:
- Python: `icalendar` (faithful parse/serialize of VEVENT properties), `httpx` (fetch). `pytest` for tests.
- C#: `Ical.Net` (parse/serialize), `System.Net.Http.HttpClient` (fetch). `xUnit` for tests.

**Storage**: Files only — `names.json` (allowlist, committed), generated `whos-out.ics` (published artifact). No database.

**Testing**: `pytest` (Python) and `xUnit` (.NET); shared fixture set of sample `.ics` inputs + expected filtered outputs.

**Target Platform**: GitHub Actions `ubuntu-latest` runner; output served via GitHub Pages. Both implementations also run locally on Windows/macOS/Linux.

**Project Type**: Command-line tool (single-purpose), two language implementations under one repo.

**Performance Goals**: Trivial — source feed is a small team calendar (order of tens–hundreds of events). A full fetch-filter-publish cycle completes in seconds; not a hot path.

**Constraints**:
- Output URL MUST stay constant across refreshes (subscription must not break).
- A failed run MUST NOT overwrite/empty the last good published feed (FR-011, FR-012).
- Source URL is sensitive (carries a feed token) → stored as a GitHub Actions **secret**, never committed.

**Scale/Scope**: One source feed → one filtered output feed. Allowlist on the order of tens of names. Runs twice daily.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The project constitution (`.specify/memory/constitution.md`) is still the **unfilled
template** — no principles have been ratified, so there are no concrete gates to
evaluate. **Status: PASS (vacuously).** No violations to justify; Complexity Tracking
left empty.

If a constitution is later ratified, re-run this gate. The design here already leans on
sensible defaults most constitutions ask for: simplicity (a small CLI, no service to
operate), test-friendliness (a pure filter function exercised by sample fixtures), and
observability (the run logs what it fetched, kept, and dropped).

**Post-Phase-1 re-check**: Still PASS. The design adds no architectural complexity — a
single pure transform plus I/O at the edges, two thin CLIs, and one workflow.

## Project Structure

### Documentation (this feature)

```text
specs/001-filter-ics-people/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   ├── cli.md                # Shared CLI contract (both implementations)
│   ├── allowlist.schema.json # JSON Schema for names.json
│   ├── config.md             # Configuration / environment contract
│   └── output-ics.md         # Output .ics contract (what is kept/preserved)
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
names.json                     # The allowlist: ["John Doe", "Richard Doe", ...]

src/
├── python/
│   ├── filter_ics/
│   │   ├── __init__.py
│   │   ├── fetch.py           # retrieve source feed from URL
│   │   ├── allowlist.py       # load + validate names.json
│   │   ├── filter.py          # extract name from SUMMARY, keep allowlisted VEVENTs
│   │   ├── render.py          # serialize filtered VCALENDAR
│   │   └── cli.py             # argument/env handling, exit codes, logging
│   └── pyproject.toml
└── dotnet/
    ├── FilterIcs/
    │   ├── Fetch.cs
    │   ├── Allowlist.cs
    │   ├── Filter.cs
    │   ├── Render.cs
    │   └── Program.cs         # CLI entry, exit codes, logging
    └── FilterIcs.csproj

tests/
├── python/                    # pytest: unit (name extraction, filtering) + integration (full run on fixture)
├── dotnet/                    # xUnit: same scenarios
└── fixtures/                  # shared sample .ics inputs + expected filtered outputs

.github/workflows/
└── publish.yml                # cron (twice daily) + workflow_dispatch: fetch → filter → deploy to Pages

docs/  (or Pages artifact)     # published whos-out.ics served by GitHub Pages
```

**Structure Decision**: Single repository, command-line tool, with two parallel
language implementations under `src/python` and `src/dotnet` that share `names.json`,
the contracts, and the `tests/fixtures` set. The GitHub Actions workflow
(`.github/workflows/publish.yml`) is the single source of scheduling and publishing and
invokes one implementation (selectable via a workflow input / config), keeping the
output URL and behavior identical regardless of which implementation runs.

## Complexity Tracking

> No constitution gates are defined, so there are no violations to justify. Table intentionally empty.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| (none)    | —          | —                                   |
