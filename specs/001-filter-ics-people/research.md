# Phase 0 Research: Filtered Time-Off Calendar Feed

All Technical Context items are resolved below. No open `NEEDS CLARIFICATION` remain.

## R1. Hosting & publishing — GitHub Pages

**Decision**: Publish the filtered `whos-out.ics` via **GitHub Pages**, deployed by the
GitHub Actions workflow using the official `actions/upload-pages-artifact` +
`actions/deploy-pages` flow.

**Stable URL**: `https://andrealonsosilva.github.io/timeoff-to-calendar/whos-out.ics`
(repo `andrealonsosilva/timeoff-to-calendar`). This URL never changes across refreshes,
so the Outlook subscription stays valid.

**Last-good preservation (FR-011/FR-012)**: A run only reaches the deploy step if fetch,
parse, and allowlist load all succeed. If any earlier step fails the job exits non-zero
**before** producing a new artifact, so the previously deployed Pages content remains
live. No empty/broken feed is ever published.

**Rationale**: No server to run or pay for; scheduling is built in (see R2); the repo is
already on GitHub. Pages keeps the last successful deployment automatically, which gives
us last-good behavior for free.

**Alternatives considered**:
- *Commit the .ics into `/docs` on `main` and let Pages serve it*: simplest, and git
  history is a natural last-good log — but creates twice-daily commit churn on `main`.
  Viable fallback if the Pages-artifact flow is undesirable.
- *Azure Function + Blob static site*: rejected for v1 — needs an Azure subscription and
  more moving parts than this single-file output warrants.
- *Local machine + Task Scheduler + OneDrive link*: rejected — requires an always-on
  machine and the share URL is less stable.

**Outlook subscription note**: Outlook for the web / new Outlook "Add calendar → Subscribe
from web" accepts the `https` Pages URL. Outlook polls subscribed internet calendars on
its own cadence (often several hours to ~a day); this is a client-side limit independent
of our twice-daily refresh and is acceptable per SC-003.

## R2. Scheduling — GitHub Actions cron

**Decision**: Trigger the workflow with two `schedule: cron` entries plus
`workflow_dispatch` for manual runs. Cron is in **UTC**.

**Chosen times**: `0 6 * * *` and `0 18 * * *` (06:00 and 18:00 UTC) — two evenly spaced
runs satisfying "twice a day" and the ≤12h freshness target (SC-003).

**Rationale**: Native to GitHub Actions, no external scheduler. `workflow_dispatch`
allows an on-demand refresh for testing and after allowlist edits.

**Known caveat (logged, not blocking)**: GitHub-hosted cron can be delayed during peak
load and is best-effort, not exact. The ≤12h requirement has wide margin, so occasional
minutes-to-low-tens-of-minutes delay is acceptable. The workflow logs its start time.

**Alternatives considered**: Self-hosted runner cron (more reliable timing but needs an
always-on machine — unjustified here).

## R3. Name extraction & matching rule

**Decision**: A source event belongs to person *N* when the **name prefix** of its
`SUMMARY` equals an allowlist entry. The name prefix is `SUMMARY` truncated at the first
` (` (space-open-paren), then trimmed.

- Example: `SUMMARY:Pedro Fernandes (Folga - 11 dias)` → name = `Pedro Fernandes`.
- If there is no ` (` in the summary, the whole trimmed `SUMMARY` is the name.

**Matching is case-insensitive and whitespace-trimmed** on both sides (FR-010). Internal
whitespace and accents are compared as-is (the feed and the allowlist both come from the
same HR system, so spellings match; accents are preserved, not stripped).

**Rationale**: Confirmed against the real `whos out.ics` sample — every event uses the
`Name (Folga - N dias)` shape and the feed carries no `ATTENDEE`/`ORGANIZER`/email, so the
summary prefix is the only available identity key (matches spec FR-009).

**Same-name limitation**: Two people sharing a display name are indistinguishable and are
kept/dropped together — an accepted limitation of a names-only feed (spec edge case).

**Alternatives considered**: Regex on the parenthetical, matching on `DESCRIPTION`, or
fuzzy name matching — all rejected as less predictable than an exact (normalized) prefix
match. Splitting on the *last* ` (` rejected because names don't contain parentheses but a
reason text theoretically could; first ` (` is the safe boundary.

## R4. Python implementation choices

**Decision**: `icalendar` for parse/serialize, `httpx` for fetch, `pytest` for tests.

**Rationale**: `icalendar` round-trips arbitrary VEVENT properties faithfully (preserves
`UID`, `DTSTART;VALUE=DATE`, `DESCRIPTION`, `CATEGORIES`, `TRANSP`, etc.), which is exactly
what FR-005 requires — keep retained events byte-for-byte meaningful. We filter the list
of `VEVENT` subcomponents and re-serialize, preserving calendar-level properties
(`VERSION`, `PRODID`, `X-WR-CALNAME`).

**Alternatives considered**: the `ics` package — higher-level but lossy on uncommon
properties and reorders fields; rejected for faithfulness. Hand-rolled line parsing —
rejected as fragile (line folding, escaping).

## R5. C# / .NET implementation choices

**Decision**: `Ical.Net` for parse/serialize, `HttpClient` for fetch, `xUnit` for tests; .NET 8.

**Rationale**: `Ical.Net` is the de-facto iCalendar library for .NET, parses to a
`Calendar` with an editable `Events` collection, and serializes back via
`CalendarSerializer`. We remove non-allowlisted events and serialize, preserving
calendar-level properties.

**Faithfulness caveat (logged)**: `Ical.Net` normalizes some serialization details
(property ordering, line folding, possibly `DTSTAMP`), so the C# output may not be
*byte-identical* to the Python output even though it is *semantically equivalent*.
Therefore equivalence between the two implementations is asserted at the **event-set +
property level** (same retained UIDs, same DTSTART/DTEND/SUMMARY/DESCRIPTION), **not** by
byte comparison. Fixture expected-output tests are written per implementation.

**Alternatives considered**: hand-rolled parsing — rejected (same reasons as Python).

## R6. Configuration & secrets

**Decision**:
- **Source URL** → GitHub Actions **secret** `SOURCE_ICS_URL` (it embeds a private feed
  token). Locally, read from the `SOURCE_ICS_URL` environment variable. Never committed.
- **Allowlist** → `names.json` committed at repo root (non-secret, human-edited).
- **Output path / calendar name** → constants/flags with sensible defaults
  (`whos-out.ics`, calendar name passed through from the source `X-WR-CALNAME`).

**Rationale**: Separates the one sensitive value from the human-edited, non-sensitive
allowlist; lets a maintainer change who is kept via a plain file edit + commit (FR-013),
which also triggers a `workflow_dispatch`/push refresh.

**Alternatives considered**: storing the URL in `names.json`/a committed config —
rejected (leaks the feed token).

## R7. Failure handling & observability

**Decision**: The CLI returns distinct non-zero exit codes per failure class (see
`contracts/cli.md`) and writes its output `.ics` **only** after a fully successful
fetch+parse+filter. The workflow gates the Pages deploy on CLI success. Each run logs:
source fetched (status, byte size), counts of events read / kept / dropped, the number of
allowlist names, and any allowlist names that matched **zero** events (a likely
typo signal).

**Rationale**: Directly enforces FR-011/FR-012 (never overwrite last-good on failure) and
makes the twice-daily runs auditable, satisfying the "record the failure" requirements.
