# Contract: Configuration & Secrets

## Source feed URL (secret)

- **Name**: `SOURCE_ICS_URL`
- **What**: The full upstream "Who's out" iCalendar URL, including any feed token.
- **Local**: provided as an environment variable.
- **CI**: stored as a GitHub Actions **repository secret** named `SOURCE_ICS_URL` and
  injected into the workflow step's `env`.
- **Never** committed to the repo and **never** logged in full (redact to scheme+host).

## Allowlist file (committed)

- **Path**: `names.json` at repo root (override with `--allowlist`).
- **Format**: see `allowlist.schema.json`.
- Editing this file and pushing is the supported way to change who is kept (FR-013); a
  push (and/or manual `workflow_dispatch`) triggers a refresh.

## Output

- **Path**: `whos-out.ics` (override with `--output`). The workflow uploads this as the
  GitHub Pages artifact.
- **Published URL (stable)**: `https://andrealonsosilva.github.io/timeoff-to-calendar/whos-out.ics`

## GitHub Pages / workflow settings

- Repository **Settings → Pages → Source = GitHub Actions**.
- Workflow `permissions`: `pages: write`, `id-token: write` (for `deploy-pages`),
  `contents: read`.
- Triggers: `schedule` (cron `0 6 * * *`, `0 18 * * *` UTC), `workflow_dispatch`, and
  `push` on `names.json` (so allowlist edits publish promptly).
