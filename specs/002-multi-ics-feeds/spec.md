# Feature Specification: Multiple Filtered Calendar Feeds

**Feature Branch**: `002-multi-ics-feeds`

**Created**: 2026-06-09

**Status**: Draft

**Input**: User description: "I want this same feature to create and publish more than one .ics files according to different .json files. Add a new folder that will contain all the .json files. Add to the json a new property called file name with a string to be used to call the .ics resulting file."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - One published feed per allowlist file (Priority: P1)

As a maintainer, I keep several allowlist files in a folder — one per group of people I care
about (e.g. a team, a squad, a project) — and the system produces and publishes a separate
filtered `.ics` for each one, named by that file's chosen output name, so each group can
subscribe to its own calendar.

**Why this priority**: This is the whole point of the change — going from one feed to many.
It is a complete, demonstrable increment on its own and supersedes the single-feed behavior.

**Independent Test**: Put two allowlist files in the folder (each naming its output and its
people), run once, and confirm two correctly named `.ics` files are published, each containing
only its own group's time-off.

**Acceptance Scenarios**:

1. **Given** a folder with two allowlist files, each specifying an output name and a set of
   people, **When** the process runs, **Then** two `.ics` files are published, each named after
   its file's output-name property and each containing only that file's people.
2. **Given** an allowlist file naming its output `engineering`, **When** the process runs,
   **Then** the published feed for that file is reachable at a stable URL ending in
   `engineering.ics`.
3. **Given** a person who appears in one allowlist file but not another, **When** the feeds are
   generated, **Then** that person's events appear only in the first file's feed.

---

### User Story 2 - Curate the set of feeds by managing files in the folder (Priority: P2)

As a maintainer, I add, remove, or edit allowlist files in the folder to control which feeds
exist and who is in each, without any code change.

**Why this priority**: Keeps the multi-feed set maintainable over time, but the core value
(P1) is demonstrable with a fixed set of files first.

**Independent Test**: Add a new allowlist file and rerun → a new feed appears; delete a file
and rerun → its feed is no longer produced.

**Acceptance Scenarios**:

1. **Given** the folder, **When** a new allowlist file is added and the process reruns, **Then**
   a new correspondingly named feed is published.
2. **Given** an existing allowlist file, **When** it is removed and the process reruns, **Then**
   that feed is no longer among the published outputs.
3. **Given** an existing allowlist file, **When** a person is added/removed and it reruns,
   **Then** that feed's contents change accordingly while other feeds are unaffected.

---

### User Story 3 - One bad file never breaks the others (Priority: P3)

As a maintainer, if one allowlist file is malformed or misconfigured, the valid feeds still
publish normally and no previously published feed is overwritten with broken/empty content.

**Why this priority**: Resilience across many files matters once there are many, but the
happy-path multi-feed publishing (P1) delivers value first.

**Independent Test**: Corrupt one allowlist file, rerun, and confirm the other feeds still
publish correctly while the bad one is skipped and reported.

**Acceptance Scenarios**:

1. **Given** one invalid and two valid allowlist files, **When** the process runs, **Then** the
   two valid feeds are published/updated and the invalid one is skipped with a recorded error.
2. **Given** a previously published feed whose file later becomes invalid, **When** the process
   runs, **Then** that feed's last good version is left in place (not emptied or removed).

---

### Edge Cases

- **Duplicate output names**: two files request the same output name → must be detected and
  reported rather than silently overwriting one feed with another.
- **Missing/empty output name**: a file has no output-name value → that file is rejected
  (it cannot produce a deterministically named feed).
- **Unsafe output name**: an output name containing path separators or other unsafe characters
  → must not escape the output location (no path traversal); rejected or sanitized.
- **Extension handling**: an output name given with or without a trailing `.ics` → the
  published file is always a single `.ics` (no double extension).
- **Empty folder / no allowlist files**: nothing to publish → the run reports zero feeds and
  does not disturb anything already published.
- **A person in a file matches zero events**: not an error; reported as a likely typo.
- **Same source feed for all**: all feeds derive from the one source; how a source failure
  affects all feeds is defined (no feed overwritten on source failure).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST read allowlist definitions from all allowlist files in a configured
  folder (each file defines one feed).
- **FR-002**: Each allowlist file MUST provide an **output-name** value (the user-named
  property "file name") used to name its resulting `.ics`, plus the set of people to keep.
- **FR-003**: System MUST produce one filtered `.ics` per allowlist file, named after that
  file's output-name (always a single `.ics`).
- **FR-004**: For each feed, System MUST keep only events belonging to a person listed in that
  feed's allowlist file, excluding everyone else (same name-matching rule as the single-feed
  feature: match on the event-title name prefix, case-insensitive, whitespace-trimmed).
- **FR-005**: System MUST publish every generated feed at a stable, distinct URL (one per
  output name) that Outlook can subscribe to; each URL MUST stay constant across refreshes.
- **FR-006**: System MUST retrieve the source calendar and apply every file's filter to it; a
  single run produces all feeds.
- **FR-007**: System MUST reject or safely handle invalid output names — empty/missing, and any
  value that would write outside the intended output location (no path traversal) — and MUST
  detect duplicate output names across files rather than silently overwriting.
- **FR-008**: A failure affecting one allowlist file (invalid file, bad output name) MUST NOT
  prevent valid feeds from being produced/published, and MUST NOT overwrite the last good
  version of any feed; the system MUST record which files failed and why.
- **FR-009**: System MUST refresh all feeds on the existing twice-daily schedule.
- **FR-010**: Adding, removing, or editing an allowlist file MUST change the set/content of
  published feeds on the next run, with no code change.
- **FR-011**: The folder-of-files model fully replaces the legacy single root allowlist. The
  legacy flat-array `names.json` is no longer processed; its content is migrated into one file
  in the new folder with an output-name. (Decision: Q2 → fully switch to the folder model.)

### Key Entities *(include if data involved)*

- **Allowlist Folder**: The configured directory holding all allowlist files; each file = one
  feed.
- **Allowlist File**: One feed definition. Attributes: an **output-name** ("file name") string,
  and the list of people (display names) to keep.
- **Output Feed**: The published filtered `.ics` for one allowlist file, served at a stable URL
  derived from the output-name.
- **Source Calendar Feed**: The single upstream feed all output feeds are derived from.
- **Person Entry**: A display name to keep, matched against event titles (unchanged from the
  single-feed feature).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Given N valid allowlist files, exactly N feeds are published, and each feed
  contains 100% of its people's events and 0% of anyone else's.
- **SC-002**: Each published feed can be independently subscribed to in Outlook at its own URL
  and shows only that group's time-off.
- **SC-003**: Adding an allowlist file results in a new published feed within one refresh cycle
  (≤12h); removing a file stops its feed.
- **SC-004**: With one malformed file among valid ones, 100% of the valid feeds are still
  published/updated and no previously published feed is emptied or removed.
- **SC-005**: Each published `.ics` file's name matches its file's output-name value (exactly
  one `.ics` extension).
- **SC-006**: The source calendar is retrieved once per run regardless of how many feeds are
  produced.

## Assumptions

- **Single shared source feed**: all feeds are filtered views of the one source calendar
  (`SOURCE_ICS_URL`); allowlist files select people, not sources.
- **Allowlist folder**: defaults to a dedicated folder (e.g. `allowlists/`); only `*.json`
  files in it are treated as feed definitions.
- **Output-name property**: the JSON property `fileName` holds a base name; a `.ics` extension
  is appended if absent, and the value is sanitized to a safe filename. (Decision: Q1 → keys
  `fileName` + `names`; chosen over the literal spaced key `"file name"` for ease of editing —
  override if you prefer the spaced form.)
- **People-list property**: each allowlist file carries the list of names under `names` (the
  same data the single-feed `names.json` held), alongside `fileName`.
- **Matching rule unchanged**: person = event-title prefix before " (", compared
  case-insensitively and trimmed.
- **Scheduling/hosting unchanged**: same twice-daily refresh and the same publishing mechanism
  as the single-feed feature, extended to publish multiple files.
- **Per-feed independence**: feeds do not reference each other; a person may appear in any
  number of files.
