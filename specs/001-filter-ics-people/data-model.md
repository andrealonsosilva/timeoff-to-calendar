# Phase 1 Data Model: Filtered Time-Off Calendar Feed

The tool is a stateless transform; there is no persistent store. These are the in-memory
/ on-disk shapes that flow through one run.

## Entity: Allowlist

The set of people to keep. On disk it is `names.json` at the repo root.

| Field | Type | Rules |
|-------|------|-------|
| (root) | array of string | Flat JSON array. Each item is one person's display name. |

**Validation**:
- MUST be a JSON array (object/other → fail with allowlist error).
- Each item MUST be a non-empty string after trimming.
- Duplicate names (after case-insensitive trim) are allowed but de-duplicated on load.
- An empty array `[]` is valid → output contains zero events.

**Normalized form (in memory)**: a set of `casefold()`-ed, trimmed names used for lookup.

**Example**:
```json
["Pedro Fernandes", "Luciano Lizzoni", "Thiago Bessa"]
```

## Entity: Source Calendar Feed

The upstream iCalendar document retrieved from `SOURCE_ICS_URL`.

| Field | Type | Notes |
|-------|------|-------|
| raw | bytes/text | The fetched `text/calendar` body. |
| calendar properties | map | `VERSION`, `PRODID`, `CALSCALE`, `X-WR-CALNAME`, top-level `UID` — preserved into output. |
| events | list of Time-Off Entry | The `VEVENT` subcomponents. |

**Validation**: MUST parse as a `VCALENDAR`. A non-2xx fetch, a body that is not valid
iCalendar, or zero `VCALENDAR` → fail (do not publish).

## Entity: Time-Off Entry (VEVENT)

One person's time-off event. Properties observed in the real feed and **preserved
verbatim** for retained events (FR-005):

| Property | Type | Notes |
|----------|------|-------|
| `UID` | string | Stable per event; preserved. |
| `SUMMARY` | string | e.g. `Pedro Fernandes (Folga - 11 dias)`. Source of the derived name. |
| `DTSTART` | date / date-time | Often `;VALUE=DATE` (all-day). Preserved with its parameters. |
| `DTEND` | date / date-time | Preserved with its parameters. |
| `DESCRIPTION` | string | e.g. `Folga (mai 18 – jun 1)`. Preserved. |
| `CATEGORIES` | string | e.g. `Quem está fora`. Preserved. |
| `DTSTAMP` | date-time | Preserved (implementation may regenerate; see research R5). |
| `TRANSP` | string | e.g. `TRANSPARENT`. Preserved. |
| any other property | — | Preserved unchanged (do not drop unknown properties). |

**Derived (not stored, computed during filtering)**:
- `personName` = `SUMMARY` up to the first ` (` , trimmed. If no ` (`, the whole trimmed `SUMMARY`.
- `kept` = `normalize(personName) ∈ Allowlist.normalized`.

**State transition**: each entry is either **kept** (copied unchanged into output) or
**dropped** (excluded). There is no modification of a kept entry's content.

## Entity: Filtered Calendar Feed (Output)

The published `whos-out.ics`.

| Field | Type | Rules |
|-------|------|-------|
| calendar properties | map | Copied from the source (same `VERSION`, `PRODID`, `X-WR-CALNAME`, etc.) so it remains a valid, recognizable calendar. |
| events | list | Exactly the **kept** Time-Off Entries, content unchanged. |

**Invariants**:
- For every output event, `normalize(personName) ∈ Allowlist`.
- No output event has a name outside the Allowlist.
- Count(output events) == Count(source events whose name ∈ Allowlist).
- Output is a well-formed `VCALENDAR` that validates for calendar subscription (SC-006).

## Entity: Run Configuration

Inputs that parameterize a single run (see `contracts/config.md`).

| Field | Source | Default |
|-------|--------|---------|
| sourceUrl | env `SOURCE_ICS_URL` / `--source-url` | (required, no default) |
| allowlistPath | `--allowlist` | `names.json` |
| outputPath | `--output` | `whos-out.ics` |

## Relationships

```
Run Configuration ──▶ fetch ──▶ Source Calendar Feed
                                      │  events
                                      ▼
        Allowlist ──▶ filter (personName ∈ Allowlist?) ──▶ kept events
                                      │
                                      ▼
                         Filtered Calendar Feed (Output) ──▶ published to GitHub Pages
```
