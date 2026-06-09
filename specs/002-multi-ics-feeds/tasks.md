---
description: "Task list for Multiple Filtered Calendar Feeds"
---

# Tasks: Multiple Filtered Calendar Feeds

**Input**: Design documents from `/specs/002-multi-ics-feeds/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: INCLUDED. The two-implementation equivalence and the manifest reconciliation /
per-feed last-good guarantees (`contracts/publishing.md`) are load-bearing, so test tasks are integral.

**Builds on**: `001-filter-ics-people` (already on `main`). This feature extends the existing
`src/python/filter_ics` and `src/dotnet/FilterIcs` code; fetch/match/serialize are reused.

**Organization**: Grouped by user story. Python (`src/python`) and C# (`src/dotnet`) tracks for
the same step are `[P]` (different files). `.github/workflows/publish.yml` is edited by
T021 → T027 → T032 — sequential (same file).

## Format: `[ID] [P?] [Story] Description`

---

## Phase 1: Setup (Shared Infrastructure)

- [x] T001 Create `allowlists/` and migrate the current `names.json` into `allowlists/whos-out.json` as `{ "fileName": "whos-out", "names": [...] }`; delete the root `names.json` (preserves the existing subscribe URL)
- [x] T002 [P] Create committed `public/` directory with an initial empty manifest `public/.feeds.json` (`{}`) and a `.gitkeep`
- [x] T003 [P] Update `.gitignore`: stop ignoring published `.ics` (remove `/whos-out.ics`; `public/*.ics` is now committed); keep secret/env patterns ignored
- [x] T004 [P] Add multi-file fixtures under `tests/fixtures/allowlists/` (e.g. `whos-out.json`, `engineering.json`) reusing `tests/fixtures/source.ics`

---

## Phase 2: Foundational (Blocking Prerequisites)

**⚠️ CRITICAL**: Building blocks every user story needs — complete before US1–US3.

- [x] T005 [P] Python: change the allowlist loader to parse one object `{ fileName, names }` and add a folder loader returning a list of feed definitions, in `src/python/filter_ics/allowlist.py`
- [x] T006 [P] C#: same object + folder loader in `src/dotnet/FilterIcs/Allowlist.cs`
- [x] T007 [P] Python: `naming` module — sanitize `fileName` to a safe single segment (no `/`,`\`,`..`), ensure exactly one `.ics`, detect duplicate output names, in `src/python/filter_ics/naming.py`
- [x] T008 [P] C#: `Naming.cs` with the same rules
- [x] T009 [P] Python: `manifest` module — read/write `public/.feeds.json` (sourcePath → outputName) in `src/python/filter_ics/manifest.py`
- [x] T010 [P] C#: `Manifest.cs`
- [x] T011 [P] Python: unit tests for naming (sanitize, dedupe, traversal rejection) in `tests/python/test_naming.py`
- [x] T012 [P] C#: `tests/dotnet/NamingTests.cs`

**Checkpoint**: Object parsing, naming/sanitization, and the manifest exist on both tracks.

---

## Phase 3: User Story 1 - One published feed per allowlist file (Priority: P1) 🎯 MVP

**Goal**: Read all allowlist files, fetch the source once, and publish one correctly named `.ics` per file.

**Independent Test**: Two valid files → two correctly named feeds, each only its people; source fetched once.

### Tests for User Story 1

- [x] T013 [P] [US1] Python: object-form allowlist parse test (`fileName` + `names`, validation) in `tests/python/test_allowlist_object.py`
- [x] T014 [P] [US1] C#: `tests/dotnet/AllowlistObjectTests.cs`
- [x] T015 [P] [US1] Python: orchestration test — folder of 2 valid files → 2 outputs with correct UIDs, source fetched exactly once (assert single fetch) in `tests/python/test_feeds_multi.py`
- [x] T016 [P] [US1] C#: `tests/dotnet/FeedsMultiTests.cs`

### Implementation for User Story 1

- [x] T017 [P] [US1] Python: `feeds` orchestration — fetch source once, for each valid feed filter (reuse 001 `filter`)+render+atomic-write `<output-dir>/<fileName>.ics`, then write the manifest, in `src/python/filter_ics/feeds.py`
- [x] T018 [P] [US1] C#: `Feeds.cs` with the same orchestration
- [x] T019 [US1] Python: switch CLI to `--allowlists-dir`/`--output-dir`, wire `feeds`, summary line, exit codes 0/1/2, in `src/python/filter_ics/cli.py` (depends on T005, T007, T009, T017)
- [x] T020 [US1] C#: update `src/dotnet/FilterIcs/Program.cs` (depends on T006, T008, T010, T018)
- [x] T021 [US1] Update `.github/workflows/publish.yml`: run the dir-based CLI into `public/`, commit the refreshed `public/` back (skip if unchanged), upload+deploy Pages; trigger on `allowlists/**` (replace the old `names.json` trigger)
- [x] T022 [US1] Update `README.md`: multiple feeds, one subscribe URL per `fileName`, the migrated `whos-out` feed, local run with the new flags

**Checkpoint**: US1 functional — multiple correctly named feeds published from the folder (MVP).

---

## Phase 4: User Story 2 - Curate the set of feeds via the folder (Priority: P2)

**Goal**: Adding/removing/editing an allowlist file changes the published feed set, no code change.

**Independent Test**: Add a file → new feed; delete a file → its feed removed (manifest reconciled).

### Tests for User Story 2

- [x] T023 [P] [US2] Python: removal/add test — manifest path no longer on disk → feed deleted & manifest entry removed; new file → new feed, in `tests/python/test_feeds_removal.py`
- [x] T024 [P] [US2] C#: `tests/dotnet/FeedsRemovalTests.cs`

### Implementation for User Story 2

- [x] T025 [P] [US2] Python: reconcile removals — delete `public/<name>.ics` for manifest paths whose source file is gone; update manifest, in `src/python/filter_ics/feeds.py` / `manifest.py`
- [x] T026 [P] [US2] C#: same reconciliation in `Feeds.cs` / `Manifest.cs`
- [x] T027 [US2] Ensure the workflow's commit-back stages deletions (so removed feeds leave `public/`); document the add/remove/edit flow in `README.md`

**Checkpoint**: Feed set is fully folder-driven; US1 + US2 work independently.

---

## Phase 5: User Story 3 - One bad file never breaks the others (Priority: P3)

**Goal**: An invalid/misconfigured file is skipped & reported; valid feeds still publish; no feed is emptied.

**Independent Test**: Corrupt one file among valid ones → valid feeds written, bad one skipped, its last-good `.ics` unchanged; duplicates and traversal rejected.

### Tests for User Story 3

- [x] T028 [P] [US3] Python: isolation test — 1 invalid + 2 valid → valid written, invalid skipped with error, errored feed's existing `.ics` byte-unchanged (G1/G3); duplicate output names both skipped (FR-007); `fileName: "../evil"` rejected (G4); exit code 0, in `tests/python/test_feeds_isolation.py`
- [x] T029 [P] [US3] C#: `tests/dotnet/FeedsIsolationTests.cs`

### Implementation for User Story 3

- [x] T030 [P] [US3] Python: per-feed error capture (skip + `error:` log, never touch existing output), duplicate-name handling, carry-over last-good, and `summary: total/written/skipped/removed` counts, in `src/python/filter_ics/feeds.py` / `cli.py`
- [x] T031 [P] [US3] C#: same in `Feeds.cs` / `Program.cs`
- [x] T032 [US3] Update `.github/workflows/publish.yml` to emit a `::warning::` annotation when the summary reports `skipped>0`

**Checkpoint**: All three stories independently functional; failures degrade gracefully.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [x] T033 [P] Cross-implementation equivalence: Python and C# produce the same feed set and per-feed contents on `tests/fixtures/allowlists/` (both suites assert the same expected outputs; event-set/property level per 001 R5)
- [x] T034 [P] Refresh `specs/002-multi-ics-feeds/quickstart.md` results; run validation scenarios V1–V11 locally where possible
- [x] T035 Run full `pytest` + `dotnet test` (build clean, no warnings); finalize `README.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (P1)** → **Foundational (P2)** → **US1 (P3)** → US2/US3 (P4/P5) → **Polish (P6)**.
- US2 and US3 both depend on Foundational + US1's `feeds` orchestration (they extend the reconcile path).

### Critical cross-file note

`.github/workflows/publish.yml` is edited by **T021 (US1) → T027 (US2) → T032 (US3)** — same file, must be sequential.

### Within each story

- Tests first (fail), then implementation. Python and C# tracks are fully parallel to each other.
- `feeds` orchestration (T017/T018) before CLI wire-up (T019/T020).

### Parallel Opportunities

- Setup: T002–T004 in parallel.
- Foundational: T005–T012 in parallel (distinct files).
- US1 tests T013–T016 parallel; modules T017/T018 parallel before CLI.
- US2 T023–T026 parallel; US3 T028–T031 parallel.
- Whole Python track ∥ whole C# track (two people).

---

## Parallel Example: User Story 1

```bash
# Tests first (parallel):
Task: "Python orchestration test in tests/python/test_feeds_multi.py"   # T015
Task: "C# orchestration test in tests/dotnet/FeedsMultiTests.cs"        # T016
# Then implementation (parallel) before CLI wire-up:
Task: "Python feeds orchestration in src/python/filter_ics/feeds.py"    # T017
Task: "C# Feeds.cs"                                                      # T018
```

---

## Implementation Strategy

### MVP First

1. Setup + Foundational.
2. US1 in one language end-to-end (e.g. Python: T005, T007, T009, T013, T015, T017, T019, T021, T022); add the C# track as its `[P]` siblings.
3. **STOP & VALIDATE**: two feeds publish with correct names; `whos-out` URL preserved.

### Incremental Delivery

Setup+Foundational → US1 (multi-feed MVP) → US2 (folder-driven add/remove) → US3 (failure isolation) → Polish (equivalence, README, validation run).

---

## Notes

- `[P]` = different files, no dependencies.
- Reuse 001's `fetch`/`filter`/`render` and matching rule unchanged — only orchestration, parsing, naming, manifest, CLI, and workflow change.
- Per-feed last-good depends on the committed `public/` + manifest (see `contracts/publishing.md`); never delete/empty a feed except on confirmed source-file removal.
- Never log the full `SOURCE_ICS_URL`.

## Completion notes (2026-06-09)

- **Done & verified locally**: all 35 tasks. 85 tests pass (44 `pytest` + 41 `xUnit`), both builds clean, no warnings. Both CLIs run with the new `--allowlists-dir`/`--output-dir` flags.
- **C# feeds tests consolidated**: T016/T024/T029 (named `FeedsMultiTests.cs`/`FeedsRemovalTests.cs`/`FeedsIsolationTests.cs` in the plan) are all implemented in one file, `tests/dotnet/FeedsTests.cs`.
- **Not verifiable locally** (needs the live repo): the workflow's commit-back of `public/` and the GitHub Pages deploy (T021/T027/T032 are written but only exercised once the workflow runs). The `SOURCE_ICS_URL` secret + Pages must be set as in 001.
