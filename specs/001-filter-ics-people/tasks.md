---
description: "Task list for Filtered Time-Off Calendar Feed"
---

# Tasks: Filtered Time-Off Calendar Feed

**Input**: Design documents from `/specs/001-filter-ics-people/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: INCLUDED. The design (plan.md testing strategy, `contracts/output-ics.md`,
and the requirement that the Python and C# implementations be equivalent) depends on a
shared fixture test set, so test tasks are integral here.

**Organization**: Tasks are grouped by user story. The feature ships **two parallel
implementations** — Python (`src/python`) and C#/.NET (`src/dotnet`) — that share
`names.json`, the contracts, and `tests/fixtures/`. Python and C# tasks for the same step
are marked `[P]` since they touch different files.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1 / US2 / US3 (maps to spec.md user stories)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project skeletons and shared assets

- [x] T001 Create repository structure per plan: `src/python/filter_ics/`, `src/dotnet/FilterIcs/`, `tests/python/`, `tests/dotnet/`, `tests/fixtures/`, `.github/workflows/`
- [x] T002 [P] Initialize Python project in `src/python/pyproject.toml` (Python 3.12; deps: `icalendar`, `httpx`; dev: `pytest`)
- [x] T003 [P] Initialize .NET 8 console project `src/dotnet/FilterIcs/FilterIcs.csproj` (dep: `Ical.Net`) and xUnit test project `tests/dotnet/FilterIcs.Tests.csproj`
- [x] T004 [P] Seed `names.json` at repo root with sample names (e.g. `["Pedro Fernandes", "Thiago Bessa"]`) per `contracts/allowlist.schema.json`
- [x] T005 [P] Configure linting/formatting: `ruff`/`black` config for Python in `src/python/pyproject.toml`; `.editorconfig` + `dotnet format` for C#

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Cross-cutting building blocks every user story needs

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T006 [P] Add shared test fixtures in `tests/fixtures/`: a representative source `whos-out.ics` and the expected filtered `.ics` for a known allowlist (basis for all fixture tests)
- [x] T007 [P] Python: CLI arg/env skeleton (`--source-url`/`SOURCE_ICS_URL`, `--allowlist`, `--output`, `--verbose`), exit-code constants (0–4), and URL-redacting logger in `src/python/filter_ics/cli.py` per `contracts/cli.md`
- [x] T008 [P] C#: CLI arg/env skeleton, exit-code constants (0–4), and URL-redacting logger in `src/dotnet/FilterIcs/Program.cs` per `contracts/cli.md`
- [x] T009 [P] Python: allowlist loader + schema validation + normalization (trim/casefold, de-dupe) in `src/python/filter_ics/allowlist.py`
- [x] T010 [P] C#: allowlist loader + validation + normalization in `src/dotnet/FilterIcs/Allowlist.cs`
- [x] T011 [P] Python: atomic output writer (temp file + move, never partial/empty) helper in `src/python/filter_ics/render.py`
- [x] T012 [P] C#: atomic output writer helper in `src/dotnet/FilterIcs/Render.cs`

**Checkpoint**: Config, allowlist loading, safe writing, and fixtures ready — stories can begin

---

## Phase 3: User Story 1 - Subscribe Outlook to a people-filtered calendar (Priority: P1) 🎯 MVP

**Goal**: Fetch the source feed, keep only allowlisted people, render a valid filtered
`.ics`, and publish it at a stable GitHub Pages URL Outlook can subscribe to (manual run).

**Independent Test**: Run once against the fixture/live feed with a 2-name allowlist;
subscribe Outlook to the Pages URL and confirm only those people appear (SC-001, SC-002).

### Tests for User Story 1

- [x] T013 [P] [US1] Python unit test: name extraction from `SUMMARY` (prefix before first ` (`; no-paren case; trim/case) in `tests/python/test_name_extraction.py`
- [x] T014 [P] [US1] C# unit test: name extraction in `tests/dotnet/NameExtractionTests.cs`
- [x] T015 [P] [US1] Python fixture test: source `.ics` + 2-name allowlist → expected kept events; assert invariants I1–I3 in `tests/python/test_filter.py`
- [x] T016 [P] [US1] C# fixture test: same scenario, event-set/property assertions per `contracts/output-ics.md` in `tests/dotnet/FilterTests.cs`

### Implementation for User Story 1

- [x] T017 [P] [US1] Python: fetch source from URL (`httpx`, timeout, non-2xx/network → exit 1) in `src/python/filter_ics/fetch.py`
- [x] T018 [P] [US1] C#: fetch via `HttpClient` (timeout, non-2xx → exit 1) in `src/dotnet/FilterIcs/Fetch.cs`
- [x] T019 [P] [US1] Python: parse iCalendar, extract name, keep allowlisted `VEVENT`s; parse failure → exit 3 in `src/python/filter_ics/filter.py`
- [x] T020 [P] [US1] C#: parse, extract name, filter; parse failure → exit 3 in `src/dotnet/FilterIcs/Filter.cs`
- [x] T021 [P] [US1] Python: render filtered `VCALENDAR` preserving calendar props (`VERSION`/`PRODID`/`X-WR-CALNAME`) and each kept event verbatim in `src/python/filter_ics/render.py`
- [x] T022 [P] [US1] C#: render filtered `Calendar` preserving props in `src/dotnet/FilterIcs/Render.cs`
- [x] T023 [US1] Python: wire CLI end-to-end (fetch→parse→filter→render→atomic write, success summary log, exit codes) in `src/python/filter_ics/cli.py` (depends on T007, T009, T011, T017, T019, T021)
- [x] T024 [US1] C#: wire CLI end-to-end in `src/dotnet/FilterIcs/Program.cs` (depends on T008, T010, T012, T018, T020, T022)
- [x] T025 [US1] Create `.github/workflows/publish.yml` with `workflow_dispatch`: run the selected implementation, then `actions/upload-pages-artifact` + `actions/deploy-pages` (Pages permissions `pages: write`, `id-token: write`); deploy gated on CLI success
- [x] T026 [US1] Document repo configuration in `README.md`: add `SOURCE_ICS_URL` secret, set Pages source = GitHub Actions, and the stable subscribe URL `https://andrealonsosilva.github.io/timeoff-to-calendar/whos-out.ics`

**Checkpoint**: US1 fully functional — a manual workflow run publishes a correctly filtered, subscribable feed (MVP)

---

## Phase 4: User Story 2 - Maintain the kept-people list in a JSON file (Priority: P2)

**Goal**: Control who is kept by editing `names.json` alone; malformed/missing allowlist
fails safely without overwriting the published feed; typos are surfaced.

**Independent Test**: Add/remove a name and rerun → output changes with no code change;
corrupt `names.json` → exit 4 and output untouched (SC-004, SC-005, FR-012, FR-013).

### Tests for User Story 2

- [x] T027 [P] [US2] Python test: missing/invalid-JSON/schema-violating `names.json` → exit 4 and output file untouched in `tests/python/test_allowlist_safety.py`
- [x] T028 [P] [US2] C# test: same safety scenarios in `tests/dotnet/AllowlistSafetyTests.cs`
- [x] T029 [P] [US2] Python test: add/remove name changes kept set; empty `[]` → zero events (I4); allowlist name with zero matches emits warning but exits 0 in `tests/python/test_allowlist_behavior.py`
- [x] T030 [P] [US2] C# test: same behavior scenarios in `tests/dotnet/AllowlistBehaviorTests.cs`

### Implementation for User Story 2

- [x] T031 [P] [US2] Python: enforce safe allowlist-error path (exit 4, never write/empty output) and emit the zero-match warning line in `src/python/filter_ics/allowlist.py` / `cli.py`
- [x] T032 [P] [US2] C#: same safe-error path and zero-match warning in `src/dotnet/FilterIcs/Allowlist.cs` / `Program.cs`
- [x] T033 [US2] Add `push` trigger on `names.json` to `.github/workflows/publish.yml` so allowlist edits republish promptly (depends on T025)
- [x] T034 [US2] Document the allowlist-editing workflow (edit `names.json` → commit/push → auto-republish) in `README.md`

**Checkpoint**: Allowlist is fully file-driven and fails safe; US1 + US2 both work independently

---

## Phase 5: User Story 3 - Keep the feed fresh automatically twice a day (Priority: P3)

**Goal**: The published feed refreshes automatically twice a day, with auditable runs and
last-good preservation on failure.

**Independent Test**: Enable the schedule (or run `workflow_dispatch`), change source data,
confirm the feed updates after a run; a failing run leaves the prior feed live (SC-003, SC-005).

### Implementation for User Story 3

- [x] T035 [US3] Add `schedule` cron `0 6 * * *` and `0 18 * * *` (UTC) to `.github/workflows/publish.yml` (depends on T025)
- [x] T036 [P] [US3] Add run-start timestamp + fetched/read/kept/dropped summary to the workflow log for auditability (research R7)
- [ ] T037 [US3] Validate a scheduled-equivalent run via `workflow_dispatch`: confirm Pages updates on success and the previous deployment is preserved on an induced failure — **PENDING: requires the live GitHub repo (`SOURCE_ICS_URL` secret + Pages enabled). Run once the one-time setup in README is done.**

**Checkpoint**: Feed self-refreshes twice daily; all three stories independently functional

---

## Phase 6: Polish & Cross-Cutting Concerns

- [x] T038 [P] Cross-implementation equivalence test: Python and C# produce the same kept event-set/property values on the shared fixture (per `contracts/output-ics.md`) in `tests/` — **realized as both suites asserting the identical expected result (`{evt-pedro-1, evt-thiago-1}`, preserved SUMMARY/DTSTART/calendar name) against `tests/fixtures/source.ics`; byte-equality intentionally not required (research R5). C# allowlist safety+behavior tests (T028/T030) are consolidated in `tests/dotnet/AllowlistTests.cs`.**
- [x] T039 [US3-adjacent] Add a workflow input to select the implementation (`python` | `dotnet`) and document switching in `README.md` (depends on T025)
- [x] T040 [P] Finalize `README.md`: overview, local run for both implementations, Outlook subscription steps
- [x] T041 Run the `quickstart.md` validation scenarios V1–V8 end-to-end and record results — **V1, V3, V4, V5, V7, V8 covered by the automated suites (42 tests green). V2 (Outlook subscription) and the live half of V6, plus the schedule check, require the deployed Pages feed (see T037).**

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Foundational — the MVP
- **US2 (Phase 4)**: Depends on Foundational; shares `publish.yml` with US1 (T033 depends on T025)
- **US3 (Phase 5)**: Depends on Foundational; edits `publish.yml` (T035 depends on T025)
- **Polish (Phase 6)**: Depends on the stories it touches

### Critical cross-file note

`.github/workflows/publish.yml` is edited by **T025 (US1) → T033 (US2) → T035 (US3) → T039**.
These are the same file and MUST be sequential (not `[P]`) relative to each other.

### Within Each User Story

- Tests written first and failing → then implementation (TDD)
- fetch / parse / render are independent `[P]` modules → CLI wiring (T023/T024) depends on them
- Python and C# tracks are fully parallel to each other

### Parallel Opportunities

- Setup: T002, T003, T004, T005 in parallel
- Foundational: T006–T012 in parallel (distinct files)
- US1 tests: T013–T016 in parallel; US1 modules: T017–T022 in parallel (then T023/T024 wire-up)
- US2 tests T027–T030 in parallel; impl T031/T032 in parallel
- Entire Python track and entire C# track can be staffed by two people in parallel

---

## Parallel Example: User Story 1

```bash
# Tests first (all parallel):
Task: "Python name-extraction unit test in tests/python/test_name_extraction.py"   # T013
Task: "C# name-extraction unit test in tests/dotnet/NameExtractionTests.cs"        # T014
Task: "Python fixture filter test in tests/python/test_filter.py"                  # T015
Task: "C# fixture filter test in tests/dotnet/FilterTests.cs"                      # T016

# Then implementation modules (all parallel) before CLI wire-up:
Task: "Python fetch in src/python/filter_ics/fetch.py"                             # T017
Task: "C# fetch in src/dotnet/FilterIcs/Fetch.cs"                                  # T018
Task: "Python filter in src/python/filter_ics/filter.py"                           # T019
Task: "C# filter in src/dotnet/FilterIcs/Filter.cs"                                # T020
```

---

## Implementation Strategy

### MVP First

1. Phase 1 Setup → Phase 2 Foundational.
2. Phase 3 (US1). The **absolute MVP** is US1 in **one** language (e.g. Python: T002, T004, T006, T007, T009, T011, T013, T015, T017, T019, T021, T023, T025, T026); the second implementation follows as its parallel `[P]` tasks.
3. **STOP and VALIDATE**: subscribe Outlook to the Pages URL and confirm filtering.

### Incremental Delivery

1. Setup + Foundational → foundation ready.
2. US1 → manual-publish MVP, subscribable in Outlook.
3. US2 → file-driven allowlist with safe failure.
4. US3 → twice-daily automation.
5. Polish → equilvalence test, README, implementation switch, quickstart run.

### Parallel Team Strategy

- Developer A owns the Python track, Developer B owns the C# track, across all stories.
- One owner serializes edits to `.github/workflows/publish.yml` (T025 → T033 → T035 → T039).

---

## Notes

- `[P]` = different files, no dependencies.
- Python and C# must stay behavior-equivalent (verified by T038 against shared fixtures).
- Never log the full `SOURCE_ICS_URL`; it carries a feed token.
- Verify tests fail before implementing; commit after each task or logical group.
