# Feature Specification: Filtered Time-Off Calendar Feed

**Feature Branch**: `001-filter-ics-people`

**Created**: 2026-06-09

**Status**: Draft

**Input**: User description: "I want to get a .ics file from a specific URL and treat the calendar file to remove people and leave only a set of people. The people that must be kept in the new .ics will be listed in a json file. Also, I want this new .ics to be reachable from my Outlook so I can subscribe a new calendar to it. Also, I want this to run twice a day and keep the .ics updated."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Subscribe Outlook to a people-filtered calendar (Priority: P1)

As someone who only cares about a subset of the team, I subscribe my Outlook calendar to a single published feed and see the time-off only for the people I selected — never anyone else.

**Why this priority**: This is the core value of the feature. Without a published, correctly filtered feed that Outlook can subscribe to, nothing else matters. It is a complete, demonstrable MVP on its own.

**Independent Test**: Configure a source feed and an allowlist, run the process once, subscribe Outlook to the resulting feed URL, and confirm the calendar shows time-off entries for allowlisted people and nothing for anyone else.

**Acceptance Scenarios**:

1. **Given** a source calendar containing time-off for many people and an allowlist naming a subset, **When** the feed is generated, **Then** the published calendar contains every source entry for allowlisted people and no entries for anyone outside the allowlist.
2. **Given** the published feed URL, **When** a user adds it as a subscribed internet calendar in Outlook, **Then** Outlook displays the filtered time-off entries without error.
3. **Given** a source entry for a person who is not in the allowlist, **When** the feed is generated, **Then** that entry does not appear in the published calendar.

---

### User Story 2 - Maintain the kept-people list in a JSON file (Priority: P2)

As the maintainer, I edit a JSON file to control exactly who is kept in the published calendar, without changing or redeploying code.

**Why this priority**: The allowlist will change over time (people join, leave, or move teams). Editing a simple file must be all that is required, but the feature can ship and demonstrate value (P1) with a fixed list before this is polished.

**Independent Test**: Add a person to the JSON file, run the process, and confirm that person now appears in the output; remove them, run again, and confirm they disappear — all without code changes.

**Acceptance Scenarios**:

1. **Given** a valid JSON allowlist, **When** a person is added and the feed regenerates, **Then** that person's time-off entries appear in the output.
2. **Given** a valid JSON allowlist, **When** a person is removed and the feed regenerates, **Then** that person's entries no longer appear.
3. **Given** a malformed or missing JSON file, **When** the feed generation runs, **Then** the run fails safely with a clear error and the previously published feed is left unchanged.

---

### User Story 3 - Keep the feed fresh automatically twice a day (Priority: P3)

As the maintainer, I want the published calendar refreshed automatically twice a day so subscribers always see reasonably current time-off without anyone running anything manually.

**Why this priority**: Automation makes the feature self-sustaining, but the filtering and publishing (P1) deliver value even when triggered manually. Scheduling is an enhancement on top.

**Independent Test**: Enable the schedule, change the source data, and confirm the published feed reflects the change after the next scheduled run (within one refresh cycle).

**Acceptance Scenarios**:

1. **Given** the schedule is enabled, **When** a scheduled run occurs, **Then** the published feed is regenerated from the current source and allowlist.
2. **Given** a change in the source calendar, **When** the next scheduled run completes, **Then** the published feed reflects that change.

---

### Edge Cases

- **Source unreachable / invalid**: The source URL times out, returns an error, or returns content that is not a valid calendar — the last good published feed MUST be preserved rather than overwritten with empty or broken content, and the failure recorded.
- **Allowlisted person has no entries**: A person is in the allowlist but has no time-off in the source — this is not an error; the output simply contains nothing for them.
- **Name formatting differences**: A person's name appears with different casing, surrounding whitespace, or accents than in the allowlist — matching should tolerate trivial differences (case, whitespace).
- **Ambiguous identity**: Two different people share the same display name in the source — since the feed exposes only names, they are indistinguishable; both are kept (or both excluded) together. This is an accepted limitation of a names-only feed.
- **Title without a parenthetical**: An entry title has no trailing ` (...)` detail — the whole title is treated as the name for matching.
- **Empty source feed**: The source returns a valid but empty calendar — the output is a valid empty calendar.
- **Empty allowlist**: The JSON file is valid but lists no one — the output is a valid calendar with no entries.
- **Updated or cancelled entries**: A time-off entry changes or is cancelled in the source between refreshes — the next refresh reflects the change for allowlisted people.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST retrieve the source calendar from a configurable source URL.
- **FR-002**: System MUST interpret the retrieved content as an iCalendar feed and read its individual time-off entries.
- **FR-003**: System MUST read the set of people to keep from a JSON file.
- **FR-004**: System MUST include in the output only entries belonging to a person named in the allowlist, and MUST exclude every entry belonging to anyone not in the allowlist.
- **FR-005**: System MUST preserve the details of each retained entry (dates/times, title, and any descriptive content) so the meaning of the time-off is unchanged in the output.
- **FR-006**: System MUST produce a valid iCalendar feed as output that standard calendar clients, including Outlook, can subscribe to.
- **FR-007**: System MUST make the output feed reachable at a stable URL that Outlook can subscribe to as an internet calendar; the URL MUST remain constant across refreshes so the subscription does not break.
- **FR-008**: System MUST regenerate the output feed automatically twice per day.
- **FR-009**: System MUST decide whether a source entry belongs to an allowlisted person by matching the person's display name. The display name is the leading portion of the entry's title before the trailing parenthetical detail (e.g., in `John Doe (Folga - 11 dias)` the name is `John Doe`). An entry is kept when its extracted name matches a name in the allowlist.
- **FR-010**: Identity matching MUST tolerate trivial differences in display name such as letter case and leading/trailing whitespace.
- **FR-011**: When the source cannot be retrieved or cannot be interpreted as a calendar, the system MUST NOT overwrite the previously published feed, and MUST record the failure.
- **FR-012**: When the JSON allowlist is missing or invalid, the system MUST fail the run safely without overwriting the previously published feed, and MUST record the failure.
- **FR-013**: System MUST allow the allowlist to be changed (people added or removed) by editing the JSON file alone, with the change taking effect on the next run.

### Key Entities *(include if feature involves data)*

- **Source Calendar Feed**: The upstream iCalendar feed retrieved from the configured URL; contains time-off entries for many people, only some of whom should be kept.
- **Time-Off Entry**: A single calendar event in the source representing one person's time off, with at least a date range, a title, and an identifier of the person it belongs to.
- **Allowlist**: The JSON file listing the people to keep — a flat array of name strings, e.g. `[ "John Doe", "Richard Doe" ]`. Each string is one person's display name exactly as it appears at the start of the entry title.
- **Person (Allowlist Entry)**: One kept individual, represented solely by their display name string. The source feed exposes no email or other stable identifier, so the name is the only matching key.
- **Filtered Calendar Feed (Output)**: The published iCalendar feed containing only the retained entries, served at a stable subscribable URL.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The output contains 100% of the source entries for allowlisted people and 0% of entries for anyone not in the allowlist.
- **SC-002**: After subscribing to the published URL in Outlook, the subscriber sees time-off only for allowlisted people, confirmed by visual inspection against the allowlist.
- **SC-003**: A change made in the source calendar is reflected in the published feed within one refresh cycle (no more than 12 hours), given the twice-daily schedule.
- **SC-004**: A maintainer can add or remove a person and see the result change after the next run, with zero code changes.
- **SC-005**: A failed source retrieval or invalid allowlist never results in a broken or emptied published feed — subscribers continue to see the last good calendar in 100% of failure cases.
- **SC-006**: The published feed validates as a well-formed iCalendar feed and is accepted by Outlook's internet-calendar subscription without error.

## Assumptions

- The source is a single iCalendar feed reachable at a URL provided via configuration (e.g., a BambooHR time-off export). Authentication to the source, if required, is supplied via configuration.
- The source feed exposes only names (no email or stable ID), so a person is identified solely by the display name at the start of each entry's title. The allowlist is therefore a flat JSON array of name strings.
- The output feed is hosted at a stable, network-reachable URL; the specific hosting mechanism is an implementation decision deferred to planning.
- "Twice a day" means approximately every 12 hours; exact run times are configurable.
- Scope for the first version is a single source feed producing a single filtered output feed.
- Time zones and all-day vs. timed semantics are preserved as they appear in the source.
- Only allowlist (keep) semantics are needed for the first version; blocklist/exclude semantics are out of scope.
