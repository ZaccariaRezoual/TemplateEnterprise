# Audit Module

Who did what, when.

## Two ways to observe, neither intrusive

1. **Pipeline behavior** — records every MediatR **command** (queries are
   skipped: recording reads would bury the actions that matter). A new module
   gets an audit trail without writing a line of auditing code, and nobody can
   forget to audit a sensitive action.
2. **Event subscribers** — records facts published by other modules on their
   public contracts, e.g. `UserLoggedIn`.

Neither requires the audited module to know auditing exists. Installing this
module adds a trail to modules written before it.

## Deliberate constraints

- **Only the command NAME is stored, never its payload.** Commands carry
  passwords and personal data; an audit table is exactly the wrong place for
  them. Payload capture, if ever needed, must be opt-in per command.
- **Failed attempts are recorded too**, then the original error propagates —
  failures are often the interesting entries.
- **A failed audit write never fails the request.** Losing an audit row is
  bad; refusing a legitimate operation because of it is worse.
- **Append-only through the application.** An audit trail that can be
  rewritten is not evidence. Retention is an operational concern
  (partitioning, archival), not an application feature.

## Endpoint

| Method | Route        | Permission   |
| ------ | ------------ | ------------ |
| GET    | `/api/audit` | `audit.read` |

Newest first, capped at 200 entries per call.
