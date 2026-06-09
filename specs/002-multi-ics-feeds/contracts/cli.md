# Contract: Command-Line Interface (multi-feed)

Replaces the single-feed CLI of 001. Both implementations expose the same interface.

## Invocation

```
filter-ics [--source-url <url>] [--allowlists-dir <dir>] [--output-dir <dir>] [--verbose]
```

| Option | Env fallback | Default | Required |
|--------|--------------|---------|----------|
| `--source-url <url>` | `SOURCE_ICS_URL` | — | Yes (flag or env) |
| `--allowlists-dir <dir>` | — | `allowlists` | No |
| `--output-dir <dir>` | — | `public` | No |
| `--verbose` | — | off | No |

## Behavior

1. Resolve config; if no source URL → exit `2`. If `--allowlists-dir` does not exist → exit `2`.
2. Enumerate `*.json` in the allowlists dir; parse each into a feed definition
   (`allowlist-file.schema.json`). Record per-file parse/validation errors (do not abort).
3. Detect duplicate resolved output names → mark all colliding files as errors.
4. **Fetch the source feed once.** On fetch/parse failure → exit `1`, write nothing.
5. For each **valid** feed: filter the source by that feed's allowlist, render, and atomically
   write `<output-dir>/<fileName>.ics` (matching rule + preservation identical to 001).
6. Reconcile against `<output-dir>/.feeds.json` (see `publishing.md`): keep errored feeds'
   last-good; delete feeds whose source file was removed; rewrite the manifest.
7. Print a summary; exit `0` if a deployable output dir was produced.

The output dir is only mutated for feeds that succeed or are intentionally removed; a failed
feed's existing file is never emptied or deleted.

## Exit codes

| Code | Meaning | Output dir |
|------|---------|-----------|
| `0` | Run completed; deployable (0+ feeds written; per-feed failures preserved last-good) | updated |
| `1` | Source fetch/parse failed | untouched (all feeds preserved) |
| `2` | Config error (missing source URL, missing allowlists dir, bad args) | untouched |

Per-feed failures do **not** change the exit code — they are reported in logs and the summary.

## Logging (stdout)

Per skipped feed (stderr):
```
error: feed <file>: <reason>   # invalid JSON / missing fileName / unsafe name / duplicate name
```
Zero-match names (per feed), as in 001:
```
warn: <fileName>: allowlist names with no matching events: ["..."]
```
Final line:
```
summary: feeds total=<N> written=<W> skipped=<S> removed=<R>, source <bytes>B
```
`--verbose` adds per-feed kept/dropped counts. The full `SOURCE_ICS_URL` is never logged
(redact to scheme+host).
