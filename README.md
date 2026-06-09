# timeoff-to-calendar

Fetch the team's "Who's out" time-off `.ics` feed and publish **one filtered calendar per
allowlist file** to **GitHub Pages**, each subscribable from Outlook. A GitHub Actions
workflow refreshes them twice a day.

- **One feed per file** in [`allowlists/`](./allowlists/): each is `{ "fileName": "...", "names": [...] }`.
- **Subscribe URL** per feed: `https://andrealonsosilva.github.io/timeoff-to-calendar/<fileName>.ics`
  (e.g. `…/whos-out.ics`).
- **Spec & design:** [`specs/002-multi-ics-feeds/`](./specs/002-multi-ics-feeds/) (builds on `001-filter-ics-people`).

## How it works

1. Fetch the source feed from `SOURCE_ICS_URL` **once**.
2. For each file in `allowlists/`, keep each event whose **person name** — the part of the
   event title before ` (`, e.g. `John Doe` in `John Doe (Folga - 11 dias)` — is in that file's
   `names` (case-insensitive, whitespace-trimmed).
3. Write one `public/<fileName>.ics` per file, preserving every kept event and the calendar name.
4. Publish `public/` to GitHub Pages. A failure affecting one file never empties another feed;
   a whole-run failure (source unreachable) leaves everything as-is.

## Allowlist files (`allowlists/*.json`)

One JSON object per feed:

```json
{ "fileName": "whos-out", "names": ["John Doe", "Jane Doe"] }
```

- `fileName` → the published file `whos-out.ics` (a `.ics` extension is added if absent).
- `names` → people to keep, exactly as they appear at the start of each event title.

To **add** a feed, add a file. To **remove** one, delete its file. To **change** who's in a
feed, edit its `names`. Push to `main` and the workflow republishes — no code changes.

> Published feeds live in the committed [`public/`](./public/) directory with a
> `public/.feeds.json` manifest. This is what lets a single bad file be skipped while every
> other feed keeps its last-good copy, and lets a deleted file remove exactly its feed.

## Two implementations

Either implementation produces identical feeds:

| | Path | Run |
|---|---|---|
| Python 3.12+ | `src/python/` | `python -m filter_ics ...` |
| C# / .NET 8+ | `src/dotnet/` | `dotnet run --project src/dotnet/FilterIcs/FilterIcs.csproj -- ...` |

> Equivalence is asserted at the **event-set + property** level (not byte-for-byte — `Ical.Net`
> and `icalendar` serialize differently). Both suites assert the same expected outputs against
> `tests/fixtures/`.

## Run locally

```powershell
$env:SOURCE_ICS_URL = "https://<host>/feed/...token..."   # never commit it — carries a token

# Python
pip install icalendar httpx
$env:PYTHONPATH = "src/python"
python -m filter_ics --allowlists-dir allowlists --output-dir public --verbose

# C#
dotnet run --project src/dotnet/FilterIcs/FilterIcs.csproj -- --allowlists-dir allowlists --output-dir public --verbose
```

Exit codes: `0` ok (deployable; per-feed problems are logged, last-good preserved) · `1` source
fetch/parse failure (nothing written) · `2` config error. The summary line reports
`total / written / skipped / removed`.

## Run the tests

```powershell
pip install icalendar httpx pytest; python -m pytest          # Python
dotnet test tests/dotnet/FilterIcs.Tests.csproj               # .NET
```

## One-time GitHub setup

1. **Settings → Secrets and variables → Actions** → add secret `SOURCE_ICS_URL` (the full
   BambooHR "Who's out" `.ics` URL, including its token).
2. **Settings → Pages → Source = GitHub Actions**.
3. Run the **Publish filtered calendars** workflow once (Actions tab → Run workflow; pick
   `python` or `dotnet`).
4. Subscribe Outlook to each feed's URL (*Add calendar → Subscribe from web*), e.g.
   `https://andrealonsosilva.github.io/timeoff-to-calendar/whos-out.ics`.

The workflow runs at **06:00 and 18:00 UTC**, on pushes that change `allowlists/**`, and on
manual dispatch. It commits the refreshed `public/` back so each feed's last-good persists.
