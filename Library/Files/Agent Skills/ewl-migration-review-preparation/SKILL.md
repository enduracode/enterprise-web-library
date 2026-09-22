---
name: ewl-migration-review-preparation
description: Prepare or refresh a file-diff-based Migration Review.md and Migration Followup.md for a large migration. Use in the migration-author session to map every added/deleted region to a multi-file review item, explain legacy origins and their fate, and hand off review to a lower-cost agent.
---

# Prepare a migration review

Typically use GPT-6 Astra in the original migration session. GPT 5.6 Terra typically
operates the resulting package with `ewl-migration-review`. The caller selects the
model; do not change models or spawn agents automatically. Use existing migration
context, but verify mappings against source rather than memory or similar names.

## Deliverables and ownership

The review package lives in the **system root beside its solution**:

- **`Migration Review.md`**: disposable review plan, file index, and item status.
  Leave it unstaged. Do not add ignore rules. Delete it only when the user confirms
  the migration is committed and review is finished.
- **`Migration Followup.md`**: durable improvements the user explicitly defers until
  after migration; intended for version control. Never stage/commit automatically.
- **`Migration Review.approvals.json`**: helper-managed full approval snapshots,
  including content, hashes, timestamps, and partial staged approvals. Keep unstaged;
  disposable with the review document after confirmed completion. Preserve existing
  evidence; preparation never creates approvals.

There is no separate legacy ledger, review subdirectory, per-item review-notes
field, or per-item approval-evidence field. The sibling JSON holds machine snapshots;
temporary baseline copies and navigation workspaces remain outside the repository.

## Establish scope and baseline

Read repository instructions; identify VCS, solution, current branch, and existing
staged/unstaged/untracked changes. Pin a full immutable **pre-migration** revision.
Include the entire migration, including already committed pieces: do not derive
scope solely from today's uncommitted diff. Inventory added, modified, deleted,
renamed, binary, and file-mode changes. Untracked new files must not be missed.

Default comparison: **fixed baseline -> working copy**. HEAD -> working copy and
HEAD -> index are additional comparisons only when explicitly requested; they do
not replace the baseline or the full migration inventory. The fixed-baseline index
view is another useful explicit option. Label comparison versions accurately.

Record all excluded changed paths and concrete reasons, not implicit exclusions
based on project/language. Include routing, markup, configuration, project entries,
and other wiring that affects behavior. Generated changes may be grouped or excluded
with a reason; edit their sources, not generated output.

No staging, commits, shelves, source changes, or rearrangement of user changes is
needed to prepare this package. Ask if baseline/scope is genuinely ambiguous. Do
not interpret staging as approval during preparation; the review skill synchronizes
that signal. Preserve approvals already established in the review workflow.

## Define review items, not single-file chunks

A **review item** is functionality approved as a unit. It may own additions,
modifications, and removals in several files. Use unique descriptive titles, not
numeric modern IDs. Order items by dependencies and reviewer convenience.

Every changed region has exactly one owning item. A region means explicit baseline
deleted lines or current added lines; a replacement includes both sides. Unchanged
diff context is not a changed region. Include imports, punctuation, formatting,
blank lines, and other mechanical changes in an appropriate item. File existence,
rename/mode changes, binary content, and empty-file changes also need one owner.

Label file entries **Review** or **Reference (not approved here)**.
Approval covers all scoped changes, including deleted methods/files. It never
propagates through an origin, caller, or cross-reference to another item's scope.
Retained unchanged legacy implementation can be a reference without needing its
own approval. Cross-reference shared explanations rather than copying them.

### Legacy origins and fate

For each origin specify baseline path, symbol, exact inclusive range, behavior
contributed, and whether coverage is complete or partial. Give full signatures for
overloads and stable anchors for unnamed sections. Include callers, markup, SQL,
or framework behavior needed to understand a port, clearly labeled as context.

Always state its fate in the **actual migrated tree**:

- **Removed by migration**: identify the scoped deletion and its owning item.
- **Remaining—in use**: identify current callers and why it is retained.
- **Remaining—inactive**: source remains but execution was disconnected.
- **Partially removed**: specify surviving versus removed ranges and their owners.
- **Unresolved**: evidence insufficient; flag for investigation, not a guessed port.

Planned later removal is still **remaining now**. Explain overlap/duplication and
whether it is intentional, temporary, or unresolved. Similar logic in retained code
does not by itself establish that duplication is acceptable.

### Independent removal audit

Inspect the legacy side independently of the identified modern ports. Account for
every deleted region and whole-file deletion, including behavior with no replacement.
For each, identify the replacement or proposed reason it no longer has a purpose.
Also inspect changed callers/routes that disconnect legacy behavior without deleting
its source. Own that loss of behavior in the item changing the caller/route.

This audit prevents useful non-migrated behavior from disappearing unnoticed. A
reverse list made only from known ports is not exhaustive. There is no separate
removal-approval command: the responsible item's approval covers these removals.

## `Migration Review.md` format

Use this structure. The file-index table is both readable and machine-checked.
Git helpers are in the companion `ewl-migration-review/scripts` directory.

```markdown
# Migration Review

## Setup
- Solution: `System.slnx`
- Baseline: `<full immutable Git commit hash>`
- Comparison: Baseline -> working copy
- Scope: ...

## File index

<!-- review-files:start -->
| Old path | Current path | Baseline lines | Current lines | Review item | File change |
|---|---|---|---|---|---|
| Library/Example.cs | Library/Example.cs | 20-24 | 20-28 | Application creation | no |
| - | Library/Storage.cs | - | 1-32 | Application creation | yes |
| Legacy/OldPage.cs | - | 1-80 | - | Application creation | yes |
<!-- review-files:end -->

## Excluded changes

<!-- review-exclusions:start -->
| Path | Reason |
|---|---|
| unrelated-document.md | Unrelated pre-existing work, outside this migration. |
<!-- review-exclusions:end -->

## File readiness
Refresh with the helper before relying on this summary; not yet checked.

## Review item: Application creation

- Status: Pending

- **Review:** `Library/Example.cs` — old 20-24 → new 20-28.
  Changed `CreateApplication` to initialize and create the application.
  Unverified: initial-value parity, validation, authorization, and failure behavior.
  - Origin: `Legacy/OldPage.cs` — old 15-40, complete `Create()` behavior.
    Removed by migration; the complete page deletion is listed below.
- **Review:** `Library/Storage.cs` — old absent → new 1-32 (complete addition).
  Added directory creation independently of the legacy assembly.
  - **Reference (not approved here):** `Legacy/Helpers.cs` — old 60-78,
    complete `CreateDirectories(string)` behavior; current 60-78.
    Remaining—in use by legacy staff workflows outside this item's scope.
    Intentional overlap: both implementations create application directories.
    Unverified: path and failure equivalence.
- **Review:** `Legacy/OldPage.cs` — old 1-80 → new absent (complete deletion).
  Removed the page; creation moved to `Library/Example.cs` above.
  Removed submission handling at old 42-65; replacement belongs to
  “Application submission,” not this item's approval.
```

Table rules: repository-relative forward-slash paths; `-` for an absent side/range;
inclusive 1-based ranges such as `4-7, 12`; plain item titles without pipes or
backticks; `yes`/`no` for ownership of the file-level change. Multiple rows can
partition a file among items. Exactly one row owns each file-level change. A detected
rename uses old/new paths in one pair; an untracked move may be reported by Git as
delete/add—use the actual helper inventory, linking both in the same item.

Ranges cover changed lines ONLY, not whole methods including unchanged context.
Empty tables retain headers and markers. Do not put follow-up prose or status in
these structured tables. `File readiness` is a convenience summary refreshed
from helper output, not proof independent of a fresh diff check.

Each item consists of its Status and ONE concise annotated file list. Do not split
it into Review scope, Legacy origins, Supporting references, Compare, or Differences
sections. Every Review entry repeats its exact changed-line ranges from the index,
with explicitly labeled old (fixed baseline) and new (working copy) sides. For an
insertion/deletion in a retained file, write “old no changed lines” or “new no changed
lines,” not “absent”; absent means the file does not exist on that side. Rename
entries show both paths. Binary, empty-file, and mode-only entries explicitly state
that line ranges are not applicable and identify the file-level change.

Attach change descriptions, origins/fate, comparison concerns, intentional
differences, and questions to the relevant file entry. Nest origins/references
beneath their replacement when helpful; combine repeated notes and cross-reference
other entries/items for shared explanations. All source references have exact
version-labeled inclusive ranges; label broader method ranges as context rather
than approval scope. Give symbols/full overloaded signatures where useful.
Unresolved ranges are explicitly unresolved, never guessed.

Describe completed changes in PAST TENSE (“Extracted,” “Added,” “Removed”), not
commands to the reviewer (“Extract,” “Check,” “Verify”). Use concise declarative
concerns such as “Unverified: concurrent creation” or “Open question: intended
source-type policy.” Current behavior/fate can use present tense. Preserve concrete
risks and user decisions; omit generic checklists, repeated history, and filler.

## Build and check the handoff

For Git systems, create the setup, empty index/exclusions tables, and named item
headings; then use `node <review-skill>/scripts/review.mjs inspect --repo <root>`.
The JSON inventory reports every changed file, changed-line ranges, file-level
changes and coverage errors. Fill the index and rerun until all in-scope lines have
one owner. The helper compares the full fixed baseline, including untracked files,
and automatically excludes the two root review documents and sibling approval JSON.
Exclusions must be explicit. Supporting references never create ownership.

Preparation does not invoke `approve`. New items start Pending even when mappings
appear correct. Explain unresolved origins and retained duplication prominently.
Validate all baseline ranges against the actual baseline, not a Fork temporary
snapshot, moving HEAD, or advisory current line numbers.

For Mercurial, load `ewl-mercurial` and perform the same inventory/audit using that
VCS. The bundled machine coverage/approval/diff helper currently requires Git. Say
so explicitly; do not promise automatic readiness certification on Mercurial.

## Durable follow-up

Create root `Migration Followup.md` if absent, initially just a heading and a brief
description when no improvements have been explicitly deferred. Each requested item
is a checkbox with improvement, modern file/symbol, rationale, desired outcome and
relevant dependencies. It must be understandable after deleting the review package:
no dependency on temporary paths, review-item status, or scratch evidence.

Current defects, incomplete mappings and questionable deletions remain review
issues unless the user explicitly defers them. Recording a follow-up never approves
an item. Neither document is automatically staged or committed.

## Refresh

Preserve user decisions and deferred work. Before refreshing an existing map, run
the review helper and identify stale mappings/changed content. Never manufacture
new approval snapshots to match changed code. Revised ownership or content may
require reapproval. Retain the complete migration inventory after commits.

