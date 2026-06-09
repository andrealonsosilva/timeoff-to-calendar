# timeoff-to-calendar

Fetch the team's "Who's out" time-off `.ics` feed, keep only the people listed in
[`names.json`](./names.json), and publish the filtered calendar to **GitHub Pages** so it
can be subscribed to from Outlook. A GitHub Actions workflow refreshes it twice a day.

- **Subscribe URL (after first publish):** `https://andrealonsosilva.github.io/timeoff-to-calendar/whos-out.ics`
- **Spec & design:** [`specs/001-filter-ics-people/`](./specs/001-filter-ics-people/)

## How it works

1. Fetch the source feed from `SOURCE_ICS_URL`.
2. Keep each event whose **person name** — the part of the event title before ` (`, e.g.
   `Pedro Fernandes` in `Pedro Fernandes (Folga - 11 dias)` — is in `names.json`
   (case-insensitive, whitespace-trimmed).
3. Write a valid filtered `.ics`, preserving every kept event and the calendar name.
4. Publish to GitHub Pages. Any fetch/parse/allowlist failure leaves the previously
   published feed untouched.

## The allowlist (`names.json`)

A flat JSON array of names, exactly as they appear at the start of each event title:

```json
["Pedro Fernandes", "Thiago Bessa"]
```

To change who appears, edit `names.json` and push to `main` — the workflow republishes
automatically. No code changes needed.

## Two implementations

The tool exists in two equivalent implementations; either can produce the feed:

| | Path | Run |
|---|---|---|
| Python 3.12+ | `src/python/` | `python -m filter_ics ...` |
| C# / .NET 8+ | `src/dotnet/` | `dotnet run --project src/dotnet/FilterIcs/FilterIcs.csproj -- ...` |

> Equivalence is asserted at the **event-set + property** level (same kept events, same
> `SUMMARY`/`DTSTART`/`DTEND`/`DESCRIPTION`), **not** byte-for-byte — `Ical.Net` and
> `icalendar` serialize formatting differently. See `specs/.../research.md` (R5). Both
> test suites assert the identical expected result against the same fixture
> (`tests/fixtures/source.ics`).

## Run locally

Set the source URL (never commit it — it carries a feed token):

```powershell
$env:SOURCE_ICS_URL = "https://<host>/feed/...token..."
```

Python:

```powershell
pip install icalendar httpx
$env:PYTHONPATH = "src/python"
python -m filter_ics --allowlist names.json --output whos-out.ics --verbose
```

C#:

```powershell
dotnet run --project src/dotnet/FilterIcs/FilterIcs.csproj -- --allowlist names.json --output whos-out.ics --verbose
```

Exit codes: `0` ok · `1` fetch error · `2` config error · `3` parse error · `4` allowlist
error. On any non-zero exit the output file is left untouched.

## Run the tests

```powershell
# Python
pip install icalendar httpx pytest
python -m pytest

# .NET
dotnet test tests/dotnet/FilterIcs.Tests.csproj
```

## One-time GitHub setup

1. **Settings → Secrets and variables → Actions** → add secret `SOURCE_ICS_URL` (the full
   BambooHR "Who's out" `.ics` URL, including its token).
2. **Settings → Pages → Source = GitHub Actions**.
3. Run the **Publish filtered calendar** workflow once (Actions tab → Run workflow). Pick
   `python` or `dotnet` via the workflow input.
4. Subscribe Outlook to the published URL above
   (*Add calendar → Subscribe from web*).

The workflow runs automatically at **06:00 and 18:00 UTC**, on pushes that change
`names.json`, and on manual dispatch.
