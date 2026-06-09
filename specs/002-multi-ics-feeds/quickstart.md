# Quickstart & Validation: Multiple Filtered Calendar Feeds

Run guide proving the multi-feed behavior end-to-end. References contracts rather than repeating them.

## Prerequisites

- The source feed URL (`SOURCE_ICS_URL`).
- Python 3.12+ or .NET 8+ (as in feature 001).
- An `allowlists/` folder with one or more feed files (`contracts/allowlist-file.schema.json`):

```jsonc
// allowlists/whos-out.json   (migrated from the old names.json)
{ "fileName": "whos-out", "names": ["John Doe", "Jane Doe"] }
// allowlists/engineering.json
{ "fileName": "engineering", "names": ["John Doe"] }
```

## Run locally

```powershell
$env:SOURCE_ICS_URL = "https://<host>/feed/...token..."
# Python
python -m filter_ics --allowlists-dir allowlists --output-dir public --verbose
# C#
dotnet run --project src/dotnet/FilterIcs/FilterIcs.csproj -- --allowlists-dir allowlists --output-dir public --verbose
```

Expected: `public/whos-out.ics` and `public/engineering.ics` are written, plus `public/.feeds.json`.
Summary line resembles:
```
summary: feeds total=2 written=2 skipped=0 removed=0, source 4821B
```

## Validation scenarios

| # | Scenario | Steps | Expected |
|---|----------|-------|----------|
| V1 (US1) | One feed per file | Two valid files, run | Two correctly named `.ics`, each only its people (SC-001, M1/M2) |
| V2 (US1) | Per-feed contents | Person in `whos-out` but not `engineering` | Appears only in `whos-out.ics` |
| V3 (US1) | Named output + extension | `fileName: "engineering"` (no ext) and `"team.ics"` (with ext) | Files `engineering.ics`, `team.ics` — never double `.ics` (SC-005) |
| V4 (US2) | Add a feed | Add a file, rerun | New `.ics` appears; manifest gains it (SC-003) |
| V5 (US2) | Remove a feed | Delete a file, rerun | Its `.ics` removed from `public/`; manifest entry gone (G2) |
| V6 (US3) | One bad file | Corrupt one file among valid ones, rerun | Valid feeds written; bad one skipped + `error:` logged; its last-good `.ics` unchanged (SC-004, G1, G3) |
| V7 (US3) | Source failure | Unreachable `SOURCE_ICS_URL`, rerun | Exit 1; **no** feed touched (all preserved) |
| V8 | Duplicate names | Two files resolve to same `fileName` | Both skipped with a clear duplicate error; neither overwrites the other (FR-007) |
| V9 | Path traversal | `fileName: "../evil"` | Rejected; nothing written outside `public/` (G4) |
| V10 | Empty folder | No `*.json` | Zero feeds; nothing published is disturbed |
| V11 | Source fetched once | Multiple feeds | Source retrieved a single time per run (SC-006) |

V1–V10 are covered by the automated suites against `tests/fixtures/allowlists/`; V11 is asserted
by a single-fetch test (the fetch is invoked once regardless of feed count).

## Publishing (twice daily)

Same secret/Pages setup as 001. The workflow runs over `allowlists/`, commits the refreshed
`public/`, and deploys; it also triggers on push to `allowlists/**`. Subscribe Outlook to each
feed's URL, e.g. `https://andrealonsosilva.github.io/timeoff-to-calendar/whos-out.ics` and
`…/engineering.ics`. The migrated `whos-out` feed keeps the original subscription URL working.
