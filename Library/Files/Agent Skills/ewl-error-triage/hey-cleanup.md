# Resolved HEY report cleanup

Use `hey-cleanup.mjs` for a user-authorized resolved-report cleanup. All network
access uses the configured System Manager MCP endpoint. No direct HEY CLI calls,
sender routing changes, sending, empty-Trash actions, or arbitrary upstream HTTP.

## Scope and authorization

First use `hey-collect.mjs discover` with a trusted local subject/sender profile to
enumerate the selected snapshot. Moving that snapshot to Set Aside is optional
for explicitly authorized cleanup; discovery itself changes no mailbox state.
Follow every returned cursor, including both Imbox sections. Keep the source
collection manifest, resolution profile, and action journal outside source control.

The resolution profile must have a confirmed `system` and `rules`. Every rule
requires `subjectPattern`, `senderPattern`, `bodyPattern`, and `installationPattern`.
Patterns are case-insensitive, trusted local regular expressions, never instructions
or filesystem paths supplied by email. Use exact diagnostic and installation
criteria. Rate-limit notifications need their own explicitly authorized rule, not
the assumption that every notification from the system has the same cause.

```json
{
  "system": "Example System",
  "rules": [{
    "subjectPattern": "^Error in Example System$",
    "senderPattern": "^errors@example\\.invalid$",
    "bodyPattern": "the explicitly resolved diagnostic signature",
    "installationPattern": "^Testing$"
  }]
}
```

## Commands

Supply the raw API token through process-local `SYSTEM_MANAGER_API_TOKEN`. The
endpoint defaults to the collection's immutable endpoint; current identity, account,
and box binding are rechecked before writes. Use the established durable state
directory, honoring `EwlFolderPath` as for the collection helper.

```powershell
node "<skill directory>\hey-cleanup.mjs" run --collection "example-errors" --profile "<absolute local resolution profile>" --authorization "User explicitly resolved diagnostic X and authorized trash plus restoration of changed threads."
node "<skill directory>\hey-cleanup.mjs" reconcile --collection "example-errors" --profile "<same profile>" --authorization "<same authorization>"
node "<skill directory>\hey-cleanup.mjs" summary --collection "example-errors" --profile "<same profile>" --authorization "<same authorization>"
```

The action journal is `collections/<name>.json.cleanup.sqlite`, bound immutably to
the source snapshot, profile, mailbox, and authorization. A separate lock records
PID and start time. Never remove a live lock; follow collection recovery guidance
for a confirmed dead process. Resume preserves completed operations. `assessed`
but unsent work is reassessed before submission. Excluded, restored-changed, and
resurfaced threads need user-directed follow-up/new discovery, not automatic replay.
Summary counts are checkpointed action outcomes, not permanent proof of current
Trash placement. Later replies can resurface completed threads; new arrivals remain
outside the frozen cleanup selection and require new discovery/review.

## Assess → trash → compare → restore

1. Confirm each topic's account and active status before reading every current
   entry and full message, following entry pagination. Only
   non-reply automated messages in Imbox/Set Aside whose entire thread matches the
   resolution rules qualify. Messages newer than the frozen source snapshot, or
   with an unknown creation timestamp, exclude the thread even if their diagnostics
   match. Register actually assessed exact messages without
   resetting prior dispositions. Record the authorized resolution for qualifying
   exact messages independently of whether the later mailbox action succeeds.
2. Retain entry IDs/kinds/update tokens and message hashes, original placement,
   timestamps, and authorization—not raw bodies. Each bounded group is assessed
   just before its mutations, avoiding a second duplicate diagnostic pass.
3. Checkpoint intent and exact-message mailbox action before each per-topic trash.
   The global request-start interval defaults to 125 ms, with four workers.
4. After trash, freshly enumerate all entries and read topic status. Compare IDs,
   kinds, and update tokens first. If the signature is unchanged but any token was
   absent, reread message contents and compare hashes. Missing/incomplete responses
   cannot establish unchanged content.
5. Any addition, removal, or change triggers `restore_topic`, followed by status
   and placement verification. Restore original Set Aside placement when applicable.
   An already active thread is left active for reassessment. Known entry changes
   trigger restoration before tokenless content comparison, so an unreadable removed
   message cannot prevent restoration. Placement verification uses a freshly listed
   surviving message, or current box postings when no messages survive. Do not
   silently retry trash or mark its new entries resolved. Seen-state preservation
   is not guaranteed.
6. Only unchanged, confirmed-trashed threads receive verified trash outcomes.
   Existing milestone notes are retained. Changed/removed existing messages reopen
   for reassessment; unchanged older messages retain their authorized resolution
    even if a new reply requires restoring the thread. For changed tokenless threads,
    old messages are provisionally reopened, then surviving originals are compared
    after restoration. Only matching hashes and unchanged entry tokens recover their
    authorized resolution. Failed comparisons remain pending and block further
    cleanup until reconciliation. A
   reply arriving after verification can resurface the thread; that new message is
   still pending work, never covered by old exact-message dispositions.

Interrupted mutations and restore failures stop further writes. Reconciliation uses
fresh reads to determine interrupted trash outcomes and never replays trash. If those
reads establish that a changed thread is still trashed, reconciliation may perform
the authorized restoration and original-placement recovery. Previously uncertain
restoration requests are checked by reads and are not automatically replayed. This
read-only branch also does not retry placement moves: a still-trashed thread, or an
active thread whose required original placement cannot be verified, remains uncertain
and requires investigation. Pending tokenless
content comparisons resume by reads before further cleanup.
Shared-topic access removal can prevent verification and must remain uncertain.

## Performance

Tracking uses one open SQLite connection, prepared statements, transactions, and
per-topic journal updates. No per-message subprocesses or whole-inventory rewrites.
Set Aside placement still uses incremental box verification. Cleanup uses exact
post-action entry comparisons, not subject-only checks or sampling unseen messages.

All reads, moves, trash, and restore operations use the existing MCP actions. No
System Manager batching update or direct HEY CLI operations are required. Up to
four ordinary reads run concurrently and share the same global pacing as mutations.
Any read failure stops new read submissions; already submitted reads may finish.

The 125 ms default permits up to eight request starts per second, versus roughly
3.3 at the previous 300 ms default. It leaves headroom below EWL's token-only
MCP connection's ten-per-second per-IP POST limit. Other requests from the same IP
share that allowance; upstream HEY limits still apply. Stop on rate limits and
reconcile uncertain mutations before continuing. `--interval-ms` remains adjustable.

The regression fixture for 1,000 one-message topics uses 6,000 ordinary MCP calls
versus the old 8,000-call two-pass cleanup, a 25% reduction. At 125 ms rather than
300 ms, the theoretical request-start budget is 12.5 rather than 40 minutes for
that fixture. This combines fewer reads with shorter pacing; it is not a measured
live-throughput result or a promise of 5–10× speedup. Actual latency, concurrency,
and service limits determine elapsed time.
