# Quickstart & Validation: Filtered Time-Off Calendar Feed

A run guide that proves the feature end-to-end. Implementation details live in `tasks.md`
and the source; contracts are referenced, not duplicated.

## Prerequisites

- The source feed URL (the BambooHR "Who's out" `.ics` link, with its token).
- For Python: Python 3.12+. For C#: .NET 8 SDK.
- A `names.json` allowlist at the repo root — see `contracts/allowlist.schema.json`.

```json
["Pedro Fernandes", "Luciano Lizzoni", "Thiago Bessa"]
```

## Run locally

Set the source URL as an environment variable (never commit it):

```powershell
# PowerShell
$env:SOURCE_ICS_URL = "https://<host>/feed/...token..."
```

Python:
```powershell
python -m filter_ics --allowlist names.json --output whos-out.ics --verbose
```

C#:
```powershell
dotnet run --project src/dotnet/FilterIcs -- --allowlist names.json --output whos-out.ics --verbose
```

Expected stdout (shape):
```
ok: fetched 4821B, read 37 events, kept 3, dropped 34, allowlist 3 names
```
Open `whos-out.ics` and confirm only the allowlisted people are present.

## Validation scenarios

These map to the spec's acceptance scenarios and success criteria.

| # | Scenario | Steps | Expected |
|---|----------|-------|----------|
| V1 (US1) | Filtering keeps only allowlisted people | Run against the sample fixture with a 2-name allowlist | Output has exactly the 2 people's events; no others (SC-001, invariants I1–I3) |
| V2 (US1) | Output subscribable in Outlook | Deploy to Pages, then Outlook → Add calendar → Subscribe from web → paste the Pages URL | Calendar named "Quem está fora" loads showing only allowlisted entries (SC-002, SC-006) |
| V3 (US2) | Add a person | Add a name to `names.json`, rerun | That person's events now appear (SC-004) |
| V4 (US2) | Remove a person | Remove a name, rerun | Their events disappear (SC-004) |
| V5 (US2) | Bad allowlist is safe | Corrupt `names.json` (invalid JSON), rerun | Exit code `4`, output file untouched (SC-005, FR-012) |
| V6 | Source failure is safe | Point `SOURCE_ICS_URL` at an unreachable/invalid URL, rerun | Exit code `1`/`3`, output file untouched (SC-005, FR-011) |
| V7 | Empty allowlist | `names.json` = `[]`, rerun | Valid empty calendar, zero events (invariant I4) |
| V8 | Typo detection | Allowlist a name not in the feed | Warning lists the zero-match name; still exit `0` |

Run the equivalence check across both implementations: same input fixture + `names.json`
⇒ same kept event set / property values (per `contracts/output-ics.md`).

## Automated publishing (twice daily)

1. Add the repo secret `SOURCE_ICS_URL` (Settings → Secrets and variables → Actions).
2. Settings → Pages → Source = **GitHub Actions**.
3. The `publish.yml` workflow runs on cron `0 6 * * *` and `0 18 * * *` (UTC), on
   `workflow_dispatch`, and on push to `names.json`.
4. After the first successful run, subscribe Outlook to:
   `https://andrealonsosilva.github.io/timeoff-to-calendar/whos-out.ics`

To validate the schedule (V-sched / SC-003): trigger `workflow_dispatch`, confirm the
Pages deployment updates, and confirm a source change appears after the run. A failing
run (V5/V6) must leave the previously deployed feed live.
