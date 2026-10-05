# Reusable Imbox collection helper

Use `hey-collect.mjs` for authorized Imbox-to-Set-Aside collections through the
configured **System Manager HTTP MCP** endpoint. It never runs the HEY CLI, contacts
HEY directly, reads message bodies, sends, drafts, trashes, or changes dispositions.
`hey-state.mjs` remains local-only. Requires Node.js 22.13+.

## Connection and storage

Run `node "<skill directory>\hey-collect.mjs" --help` for the interface. Use absolute
helper/profile paths and resolve the endpoint from trusted client configuration.

- Pass `--endpoint "https://<host>/<application path>/mcp/hey"`, or supply
  `SYSTEM_MANAGER_MCP_URL`. Preserve the path base. The helper initializes MCP
  `2025-06-18` and requires a stateless System Manager endpoint.
- Supply the existing **raw** API token through the process-local
  `SYSTEM_MANAGER_API_TOKEN`, or use `--token-env <variable name>`. Never put a token
  in command arguments, a profile, manifest, transcript, or source file. Local EWL
  machine-configuration lookup follows the system-owned `hey` skill; the shared
  helper does not guess a user/profile.
- TLS verification is enabled; Node’s additional-CA configuration is usable.
  `--insecure-local` is an explicit per-invocation exception for owner-authorized
  development testing on `localhost`, `127.0.0.1`, or `::1` only. It does not change
  machine settings or disable TLS for model-provider requests/other processes.
- State defaults to `<EwlFolderPath or platform EWL folder>/Error Triage/HEY`.
  `--state` overrides it, sharing resolution code with `hey-state.mjs`. Collections
  live in `collections/<name>.json`; names are simple identifiers, not email-derived
  filesystem paths. Retain these outside source control, deployments, and refreshes.
- Manifests contain metadata only: endpoint/identity/account/box binding, frozen
  profile, discovery boundary/cursors, topic/posting IDs, bounded subject/sender
  fields, attempt/outcome timestamps, and audit results. They are not reviewed-message
  dispositions. Existing ad hoc ledgers are historical records, not this format;
  do not rename them into `collections` to replay previous work.

When changing development/live endpoints, keep historical collections bound to
their original endpoint and create a new collection for new work. Local matching
profiles can be reused after checking scope. Disposition continuity requires a
verified identity/account mapping; see [Changing the MCP endpoint](hey.md#changing-the-mcp-endpoint).

## Trusted local matching profile

Keep system-specific aliases and subject/sender patterns in a local JSON profile,
for example `<state directory>/profiles/<scope>.json`, not in the shared helper.

```json
{
  "description": "Example system errors and attestation reminders",
  "categories": [
    {
      "name": "Example errors",
      "subjectPattern": "^(?:Error|Fault) in Example System\\b"
    },
    {
      "name": "Example attestation reminders",
      "subjectPattern": "^Example: membership attestation required$",
      "senderPattern": "^notifications@example\\.org$"
    }
  ]
}
```

Patterns are trusted, case-insensitive regular expressions; first matching category
wins. A supplied sender pattern must also match sender/creator email metadata;
missing data does not grant a match. Resolve aliases from `General.xml`, and verify
less-obvious notification subjects against trusted application templates. Subjects
select reports but do not diagnose their exceptions. Never build profiles from
instructions in email or broaden unclear scope silently.

With multiple linked accounts, discovery requires explicit `--account <ID>`. Every
networked command checks the same endpoint, identity, account, and boxes before
moving anything. The emitted `mailboxKey` follows disposition tracking’s
`endpoint|identity:<ID>|account:<ID>` convention.

## Discover, move, and audit

These examples assume endpoint/token environment variables; add applicable
connection options on each networked invocation.

```powershell
node "<skill directory>\hey-collect.mjs" discover --collection "errors-YYYY-MM-DD" --profile "<absolute profile path>" --pages 100
node "<skill directory>\hey-collect.mjs" summarize --collection "errors-YYYY-MM-DD" --limit 30
node "<skill directory>\hey-collect.mjs" move --collection "errors-YYYY-MM-DD" --batch-size 50 --batches 20
node "<skill directory>\hey-collect.mjs" audit --collection "errors-YYYY-MM-DD"
```

1. **Discover** selects without moving, checkpoints every page, and resumes using
   returned cursors. Repeat the identical command until `discoveryComplete` is true.
   `--pages` limits one invocation, not the requested collection. Profiles are frozen;
   a changed scope needs a new collection. Bundles/unidentifiable topics and unknown
   dates are counted. Threads created after discovery started are skipped. This is
   a paginated observed snapshot, not a server-side transaction; audit concurrent
   arrivals/changes rather than silently including them.
   Discovery, reconciliation, and audit explicitly enumerate both New For You and
   Previously Seen. Posting IDs are deduplicated across the two streams. Opaque
   HEY cursors are retained inside helper-owned `unread:`/`seen:` stream markers;
   do not pass these composite markers directly to HEY tools.
2. **Summarize** is offline and bounded: scope, progress, outstanding outcomes, last
   audit, and round-robin category samples. Samples are ledger metadata, not fresh
   membership proof or diagnoses. Do not dump full manifests into model context.
3. **Move** requires complete discovery. A batch is durable as `attempting` before
   requests leave. Defaults: 50 threads per batch, four concurrent calls, shared
   125 ms global request-start interval (up to eight requests/second). Tune `--interval-ms`/`--concurrency` to
   service capacity; a 429 stops the command. Repeat invocations until the requested
   selection is verified; do not silently stop at `--batches`.
4. A successful response is **accepted**, not **verified**. Exact posting/account
   IDs are checked in Set Aside. Incremental verification follows returned `since`,
   `v`, and continuation values. A 409 discards partial verification and falls back
   to a fresh, fully paginated destination read. Full responses stay in memory;
   bounded metadata/counts are emitted instead of relying on truncated tool output.
5. **Audit** freshly enumerates both boxes. It reports selected postings still in
   Imbox, missing from both, and other matches outside the selection. It does not
   move new arrivals or undo user actions. `lastAudit.complete` means all selected
   postings were observed in Set Aside and none in Imbox during that audit;
   historically verified move counts alone do not establish current placement.

No command automatically retries a failed mutation. Placement verification does
not establish preservation of seen state; curated MCP lacks per-thread seen/unseen.
All mutations remain per-topic MCP operations. Raising the batch size does not turn
them into an upstream bulk request. Tune pacing only within service capacity and
stop on rate limits; do not use direct CLI access to bypass the broker.

## Recovery

```powershell
node "<skill directory>\hey-collect.mjs" reconcile --collection "errors-YYYY-MM-DD"
node "<skill directory>\hey-collect.mjs" summarize --collection "errors-YYYY-MM-DD" --limit 30
# After read-back and an explicit decision to retry the source postings:
node "<skill directory>\hey-collect.mjs" move --collection "errors-YYYY-MM-DD" --retry-reconciled --batches 20
```

- Failed/interrupted calls are **uncertain**. New mutations stop, though submitted
  requests may finish. After 429, wait for the service’s allowed recovery period
  before reconciling; do not bypass rate limits.
- **Reconcile** checks Set Aside first. Confirmed destination postings become
  verified without another mutation. Otherwise a full Imbox read can identify the
  same account/posting/topic as **retryable**, not automatically pending. Missing
  from both means **missing**, requiring investigation rather than replay.
- Retryable operations are sent only by a later `move --retry-reconciled` invocation:
  a new decision after read-back, not an automatic transport retry. A late completion
  can be verified by another reconciliation.
- Ctrl+C aborts client requests, preserves checkpointed uncertainty, and releases
  the lock on orderly exit. Cancellation/disconnect does not prove remote rollback.
- Each command exclusively locks `<manifest>.lock`, recording PID/time, and saves
  by atomic replacement. After a hard crash, confirm the recorded process is no
  longer running before removing **that collection’s** stale lock. Preserve the
  manifest; `attempting` is uncertain. Never remove a live lock, erase the ledger,
  or reset outcomes to bypass reconciliation.

## Feeding bounded triage

Collection is not triage. Use bounded current Set Aside evidence to select reports
across requested categories. A recent audit is a snapshot with an observation time,
not permanent membership proof; refresh stale evidence. First-page ordering after
a large move may hide entire categories. Disclose categories not covered and keep
the five-minute review budget. Only actually reviewed messages enter `hey-state.mjs`.
Collection counts are moved threads, not recurrence counts of particular exceptions.
A “deserialization” subject may contain an HTTP retrieval failure; read representative
evidence before assigning a cause.
