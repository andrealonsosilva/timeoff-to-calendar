# Contract: Output `.ics`

Defines what the filtered calendar must contain. Verified by fixture tests (same input
`.ics` + `names.json` → expected events).

## Calendar level

- Output MUST be a single well-formed `VCALENDAR`.
- These source calendar properties MUST be preserved: `VERSION`, `PRODID`, `CALSCALE`,
  `X-WR-CALNAME`, and the calendar `UID` if present. (The subscriber sees the same
  calendar name, e.g. `Quem está fora`.)

## Event selection rule

For each source `VEVENT`:

1. Compute `personName` = `SUMMARY` truncated at the first occurrence of ` (` (space +
   open paren), then trimmed. If ` (` is absent, `personName` = trimmed `SUMMARY`.
2. Normalize by trimming and case-folding both `personName` and each allowlist entry.
3. **Keep** the event iff its normalized `personName` is in the normalized allowlist;
   otherwise **drop** it.

## Preservation rule (kept events)

A kept event's content MUST be carried over unchanged: `UID`, `SUMMARY`, `DTSTART` (with
its `VALUE=DATE`/timezone parameters), `DTEND`, `DESCRIPTION`, `CATEGORIES`, `TRANSP`, and
any other properties present on the source event. No property is dropped or rewritten.

> Implementation note (research R5): `Ical.Net` (C#) may re-serialize with different
> property ordering / line folding and may regenerate `DTSTAMP`. Equivalence between the
> Python and C# outputs is therefore asserted at the **event-set + property-value** level
> (same kept `UID`s; identical `SUMMARY`, `DTSTART`, `DTEND`, `DESCRIPTION`), **not** by
> byte comparison.

## Invariants (testable)

- I1: Every output event's normalized `personName` ∈ allowlist.
- I2: No source event with name ∈ allowlist is missing from the output.
- I3: `count(output events) == count(source events with name ∈ allowlist)`.
- I4: Empty allowlist ⇒ zero output events, still a valid `VCALENDAR`.
- I5: Output parses cleanly with the same library that produced it and validates for
  calendar subscription.

## Worked example

Source (excerpt):
```
SUMMARY:John Doe (Folga - 11 dias)   ← name "John Doe"
SUMMARY:Richard Doe (Folga - 15 dias)   ← name "Richard Doe"
SUMMARY:Jane Doe (Folga - 20 dias)      ← name "Jane Doe"
```
`names.json` = `["John Doe", "Jane Doe"]`

Output contains the **John Doe** and **Jane Doe** events unchanged; the
**Richard Doe** event is dropped.
