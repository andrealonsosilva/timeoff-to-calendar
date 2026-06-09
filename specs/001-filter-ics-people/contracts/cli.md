# Contract: Command-Line Interface

Both implementations (Python and C#) expose the **same** CLI so the GitHub Actions
workflow can call either interchangeably.

## Invocation

```
filter-ics [--source-url <url>] [--allowlist <path>] [--output <path>] [--verbose]
```

- Python: `python -m filter_ics ...`
- C#: `dotnet run --project src/dotnet/FilterIcs -- ...` (or the built executable)

## Arguments & configuration

| Option | Env fallback | Default | Required |
|--------|--------------|---------|----------|
| `--source-url <url>` | `SOURCE_ICS_URL` | — | Yes (via flag or env) |
| `--allowlist <path>` | — | `names.json` | No |
| `--output <path>` | — | `whos-out.ics` | No |
| `--verbose` | — | off | No |

If `--source-url` is absent, the value MUST be read from `SOURCE_ICS_URL`. If neither is
present → exit code `2` (configuration error).

## Behavior

1. Load and validate the allowlist (`contracts/allowlist.schema.json`).
2. Fetch the source feed from the URL.
3. Parse it as iCalendar.
4. Keep only events whose derived person name is in the allowlist (`contracts/output-ics.md`).
5. **Only on full success**, write the filtered calendar to `--output` (atomically:
   write to a temp file, then move into place — never leave a partial/empty output).
6. Print a one-line summary to stdout (see Logging).

The output file MUST NOT be created or modified if any of steps 1–4 fail.

## Exit codes

| Code | Meaning | Output file |
|------|---------|-------------|
| `0` | Success — filtered feed written | written/updated |
| `1` | Source fetch failed (network error, non-2xx, timeout) | untouched |
| `2` | Configuration error (missing source URL, bad arguments) | untouched |
| `3` | Source parse error (body is not valid iCalendar) | untouched |
| `4` | Allowlist error (missing file, invalid JSON, schema violation) | untouched |

Any non-zero exit MUST leave the previously published output untouched (FR-011/FR-012),
which the workflow relies on to preserve the last-good Pages deployment.

## Logging (stdout, structured one-liner + optional detail)

On success, emit at minimum:
```
ok: fetched <bytes>B, read <N> events, kept <K>, dropped <D>, allowlist <M> names
```
If any allowlist name matched **zero** events, also emit a warning line listing them
(likely typos), but still exit `0`:
```
warn: allowlist names with no matching events: ["..."]
```
On failure, emit `error: <class>: <message>` to stderr and exit with the matching code.

`--verbose` additionally logs each dropped/kept name. Logs MUST NOT print the full
`SOURCE_ICS_URL` (it carries a token); redact to scheme+host.
