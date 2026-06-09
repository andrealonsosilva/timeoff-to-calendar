<!-- SPECKIT START -->
## Active feature: Filtered Time-Off Calendar Feed (`001-filter-ics-people`)

Current plan: `specs/001-filter-ics-people/plan.md`
(spec: `specs/001-filter-ics-people/spec.md`; design: `research.md`, `data-model.md`, `contracts/`, `quickstart.md`)

- **What**: Fetch the upstream "Who's out" `.ics` from `SOURCE_ICS_URL`, keep only events
  whose person name (SUMMARY prefix before ` (`) is in `names.json`, publish the filtered
  `.ics` to GitHub Pages, refreshed twice daily by a GitHub Actions workflow.
- **Stack**: planned in **both** Python 3.12 (`icalendar` + `httpx`, pytest) and C#/.NET 8
  (`Ical.Net` + `HttpClient`, xUnit), under `src/python` and `src/dotnet`, sharing
  `names.json`, contracts, and `tests/fixtures`.
- **Publish**: GitHub Pages via `actions/upload-pages-artifact` + `deploy-pages`; stable URL
  `https://andrealonsosilva.github.io/timeoff-to-calendar/whos-out.ics`.
- **Secrets**: `SOURCE_ICS_URL` is a GitHub Actions secret (carries a feed token) — never
  commit or log it in full.
- **Safety**: any fetch/parse/allowlist failure MUST NOT overwrite the last-good output
  (CLI exit codes 1–4; deploy gated on success).
<!-- SPECKIT END -->
