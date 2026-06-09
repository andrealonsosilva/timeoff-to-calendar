# Contract: Publishing model (committed `public/` + manifest)

Defines how feeds are persisted so per-feed last-good survives failures (FR-008) while removals
take effect (FR-010). Verified by reconciliation tests.

## Committed output directory

- `public/` is **committed** to the repo and is what GitHub Pages serves.
- Each feed is `public/<fileName>.ics`. URLs: `…github.io/timeoff-to-calendar/<fileName>.ics`.
- A manifest `public/.feeds.json` maps each allowlist **source path** → its last-known output name:
  ```json
  { "allowlists/whos-out.json": "whos-out", "allowlists/engineering.json": "engineering" }
  ```

## Reconciliation algorithm (per run)

Given: `present` = `*.json` files on disk; `manifest` = previous run's map; `valid`/`errored`
partition of `present` after parsing.

For each `path` in `present`:
- **valid** → write `public/<outputName>.ics` (atomic); set `manifest[path] = outputName`.
- **errored** and `path` in `manifest` → **keep** `public/<manifest[path]>.ics` untouched (last-good).
- **errored** and `path` not in `manifest` → nothing to preserve; skip.

For each `path` in `manifest` **not** in `present` (file removed):
- **delete** `public/<manifest[path]>.ics`; remove the entry.

Finally rewrite `public/.feeds.json`.

## Guarantees (testable)

- **G1 (last-good)**: if a file is present but invalid, its previously published `.ics` is byte-for-byte unchanged.
- **G2 (removal)**: if a file is deleted from `allowlists/`, its feed is removed from `public/` and the manifest.
- **G3 (isolation)**: a feed write/skip never alters another feed's file.
- **G4 (no traversal)**: every written/deleted path is inside `output-dir` (sanitized names only).
- **G5 (atomicity)**: a feed file is never left partial — temp-write then replace.

## Workflow integration

The publish workflow:
1. Checks out the repo (brings in last-good `public/`).
2. Runs the tool over `allowlists/` writing into `public/`.
3. Commits the refreshed `public/` (feeds + `.feeds.json`) back — skip commit if no changes.
4. Uploads `public/` as the Pages artifact and deploys.
5. Emits a `::warning::` annotation if the summary reports `skipped>0`.

Triggers: twice-daily cron + `workflow_dispatch` + push on `allowlists/**`. (The old push
trigger on root `names.json` is removed; that file is retired.)
