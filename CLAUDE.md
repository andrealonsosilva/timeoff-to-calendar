<!-- SPECKIT START -->
## Active feature: Multiple Filtered Calendar Feeds (`002-multi-ics-feeds`)

Current plan: `specs/002-multi-ics-feeds/plan.md`
(spec: `specs/002-multi-ics-feeds/spec.md`; design: `research.md`, `data-model.md`, `contracts/`, `quickstart.md`)
Builds on `001-filter-ics-people` (single-feed; same fetch/match/serialize, Python + C#).

- **What**: produce **many** filtered `.ics` from **many** allowlist files in `allowlists/`.
  Each file is `{ "fileName": "...", "names": [...] }` → publishes `public/<fileName>.ics`.
  Source fetched **once**, filtered per file. (Q1 keys `fileName`/`names`, Q2 full switch to
  folder model — recommended defaults, unconfirmed; easy to override.)
- **Migration**: legacy flat `names.json` retired → `allowlists/whos-out.json`
  (`fileName: "whos-out"`) so the existing subscribe URL keeps working.
- **Publishing**: `public/` is **committed** + a `public/.feeds.json` manifest. Per-feed
  last-good: valid feeds overwrite, errored feeds keep their last-good, removed files delete
  their feed. Workflow commits refreshed `public/` then deploys Pages; triggers on `allowlists/**`.
- **CLI**: `filter-ics --allowlists-dir allowlists --output-dir public`. Exit `0` deployable,
  `1` source failure, `2` config; per-feed failures degrade gracefully (logged, last-good kept).
- **Secrets**: `SOURCE_ICS_URL` GitHub Actions secret — never commit/log in full.
<!-- SPECKIT END -->
