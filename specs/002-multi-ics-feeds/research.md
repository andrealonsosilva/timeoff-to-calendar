# Phase 0 Research: Multiple Filtered Calendar Feeds

Builds on `001-filter-ics-people`. Only the multi-feed deltas are researched here; the source
fetch, the name-matching rule, and the calendar parse/serialize libraries are unchanged
(see 001 research R3–R5). All Technical Context unknowns are resolved below.

## R1. Allowlist file format & folder layout

**Decision**: Each feed is one JSON file in `allowlists/`, shaped:
```json
{ "fileName": "engineering", "names": ["John Doe", "Jane Doe"] }
```
- `fileName`: required, non-empty string — the output base name.
- `names`: required array of non-empty strings — same semantics as the old `names.json`.
- Only `*.json` files in the folder are processed; other files ignored.

**Rationale**: Minimal change from the flat array — the `names` array is exactly the old
content, plus one `fileName` key. `fileName`/`names` chosen over the literal `"file name"`
(spaced key) for editing convenience (Q1); overriding is a one-line schema/parse change.

**Migration (Q2)**: The legacy flat-array `names.json` is retired. The current real list moves
to `allowlists/whos-out.json` with `fileName: "whos-out"`, which **preserves the existing
published URL** `…/whos-out.ics` and its Outlook subscription.

**Alternatives considered**: one big file with an array of feed objects (rejected — a folder of
files is easier to add/remove/diff and matches the user's request); keeping the flat array with
a sidecar name (rejected — two formats).

## R2. Publishing model — committed `public/` + manifest (enables per-feed last-good)

**Decision**: Treat `public/` as **committed repository state** that GitHub Pages serves. Each
run:
1. Starts from the checked-out `public/` (= last-good for every feed).
2. Overwrites `public/<fileName>.ics` **only** for feeds that regenerate successfully (atomic write).
3. Leaves an errored file's existing feed untouched (carry-over = do nothing).
4. Deletes `public/<name>.ics` only for feeds whose **source file was removed** (detected via the manifest).
5. Rewrites `public/.feeds.json` and commits the refreshed `public/` back, then deploys to Pages.

**Why a manifest**: To distinguish *removed* from *errored*. `public/.feeds.json` maps each
allowlist source-file path → its last-known `fileName`. On a run, a present-but-invalid file is
keyed by its **path** (still on disk) → we look up its previous `fileName` and keep that feed's
last-good. A manifest entry whose source path no longer exists = an intentional removal → delete
that feed. This satisfies US2 (removal stops a feed) and US3/FR-008 (errors never empty a feed)
simultaneously.

**Rationale**: GitHub Pages serves exactly what is deployed; an artifact-only deploy (as in 001)
would drop any feed not regenerated this run, violating per-feed last-good. Committing `public/`
makes last-good durable and the deploy deterministic. The cost is twice-daily commit churn on
`public/`, which is acceptable for this low-traffic repo and is called out as the one tradeoff.

**Alternatives considered**:
- *Artifact-only, re-download missing feeds from the live Pages URL on failure* — fragile
  (depends on the live site, races with deploy). Rejected.
- *Abort the whole deploy if any feed fails* — violates "valid feeds still publish" (SC-004). Rejected.
- *Serve from a `gh-pages` branch* — equivalent to committing `public/`; chose `public/` on the
  working branch for simplicity. Acceptable either way at implementation time.

## R3. Output-name sanitization, extension, and duplicate handling

**Decision**:
- Sanitize `fileName` to a safe single path segment: allow letters/digits/`-`/`_`; reject or
  strip path separators (`/`, `\`), `..`, leading dots, and empties → **no path traversal**
  (FR-007). An unsafe/empty `fileName` makes that file an error (skipped, recorded).
- Always emit exactly one `.ics`: if `fileName` already ends in `.ics`, don't double it.
- **Duplicate output names**: if two valid files resolve to the same sanitized `<fileName>.ics`,
  both are treated as errors and skipped with a clear message — never silently overwrite one feed
  with another.

**Rationale**: Output names become URLs and file paths; they must be deterministic and confined
to `public/`. Failing duplicates loudly avoids a feed silently shadowing another.

## R4. Per-feed failure isolation & exit semantics

**Decision**:
- The CLI loads all files, **fetches the source once**, then processes each feed independently.
- A per-feed problem (invalid JSON, missing/empty/unsafe `fileName`, duplicate name) is recorded
  and skipped; remaining feeds still publish. The errored feed's last-good is preserved (R2).
- Exit codes:
  - `0` — run produced a deployable `public/` (zero or more feeds updated; any failures are
    per-feed and last-good was preserved). **Deploy proceeds.**
  - `1` — source fetch/parse failed → nothing written, all feeds preserved, **no deploy**.
  - `2` — config error (missing `SOURCE_ICS_URL`, missing allowlists dir, bad args) → no deploy.
- Per-feed failures are surfaced as `error:` log lines plus a final
  `summary: feeds total=… written=… skipped=…` line. The workflow emits a `::warning::`
  annotation when `skipped>0` so a partial failure is visible without blocking the good feeds.

**Rationale**: Directly encodes FR-008/SC-004 — whole-run failures preserve everything and don't
deploy; per-feed failures degrade gracefully while keeping the deploy green and visible.

## R5. Scheduling, hosting, secrets, and the two implementations — unchanged

Same twice-daily GitHub Actions cron + `workflow_dispatch` + push trigger, the same
`SOURCE_ICS_URL` secret, and the same Python/C# equivalence approach (assert identical kept
event-sets/property values per fixture; not byte-equality). The workflow gains a commit-back step
for `public/` (R2) and a trigger on `allowlists/**` so adding/editing a feed republishes
promptly. The push trigger on the old root `names.json` is removed (file retired).
