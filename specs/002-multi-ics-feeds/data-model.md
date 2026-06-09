# Phase 1 Data Model: Multiple Filtered Calendar Feeds

Extends 001's model (Source Feed, Time-Off Entry, Output Feed unchanged). New/changed shapes below.

## Entity: Allowlist File (CHANGED)

One feed definition, one JSON file in `allowlists/`.

| Field | Type | Rules |
|-------|------|-------|
| `fileName` | string | Required, non-empty. Output base name. Sanitized to a safe single path segment (letters/digits/`-`/`_`); no path separators or `..`. |
| `names` | array of string | Required. Each a non-empty display name (same as the old `names.json`). De-duplicated case-insensitively in memory. |

**Validation** (any failure → that file is an *error*, skipped, recorded; never aborts other feeds):
- Root MUST be a JSON object (not an array/scalar).
- `fileName` present, string, non-empty after sanitization, safe (no traversal).
- `names` present and an array of non-empty strings (empty array is allowed → an empty feed).
- The resolved `<fileName>.ics` MUST be unique across all files this run (duplicates → error for all colliding files).

**Example** — `allowlists/engineering.json`:
```json
{ "fileName": "engineering", "names": ["John Doe", "Jane Doe"] }
```

## Entity: Allowlist Folder

The configured directory (default `allowlists/`). Source of feed definitions.

| Field | Type | Notes |
|-------|------|-------|
| path | dir | Default `allowlists/`; only `*.json` entries are feed definitions. |
| files | list of Allowlist File | Zero or more. Empty folder → zero feeds (valid, nothing published). |

## Entity: Feed Definition (in memory)

Result of parsing one valid Allowlist File.

| Field | Type | Notes |
|-------|------|-------|
| sourcePath | string | Relative path of the JSON file (manifest key). |
| outputName | string | Sanitized `fileName` (without extension). |
| allowlist | normalized name set | As in 001 (trim + casefold). |

## Entity: Output Feed (per feed)

The published `public/<outputName>.ics`. Calendar-level properties and kept-event preservation
are exactly as in 001 (`contracts/output-ics.md` of feature 001). Selection uses this feed's
own allowlist.

**Invariants** (per feed): I1–I5 from 001, scoped to that feed's allowlist. Plus:
- M1: `count(published feeds)` == `count(distinct valid output names this run)` (after removals).
- M2: No two published feeds share a file name.

## Entity: Feed Manifest (NEW) — `public/.feeds.json`

Durable map used to detect intentional removals vs. transient errors.

| Field | Type | Notes |
|-------|------|-------|
| entries | object | Maps `sourcePath` → last-known `outputName`. |

**Example**:
```json
{ "allowlists/whos-out.json": "whos-out", "allowlists/engineering.json": "engineering" }
```

**State transitions per run** (keyed by `sourcePath`, which exists on disk even when content is invalid):

| Situation | Action on `public/<name>.ics` | Manifest |
|-----------|-------------------------------|----------|
| File valid | Regenerate & atomically overwrite | upsert path → outputName |
| File present but invalid, path known in manifest | **Keep last-good** (no write) | keep entry |
| File present but invalid, path unknown (new + broken) | Nothing to preserve; skip | no entry |
| Manifest path no longer on disk (file removed) | **Delete** the feed | remove entry |

## Entity: Run Configuration (CHANGED)

| Field | Source | Default |
|-------|--------|---------|
| sourceUrl | env `SOURCE_ICS_URL` / `--source-url` | required |
| allowlistsDir | `--allowlists-dir` | `allowlists` |
| outputDir | `--output-dir` | `public` |

## Relationships

```
Run Config ─▶ fetch (ONCE) ─▶ Source Feed
                                   │
Allowlist Folder ─▶ [Allowlist File]→ parse ─▶ [Feed Definition]
                                   │                 │ for each (filter source by this allowlist)
                                   ▼                 ▼
                          Feed Manifest ◀── reconcile ──▶ public/<name>.ics  (write / keep / delete)
                                                   │
                                                   ▼
                                      committed public/ ─▶ GitHub Pages (N subscribable URLs)
```
