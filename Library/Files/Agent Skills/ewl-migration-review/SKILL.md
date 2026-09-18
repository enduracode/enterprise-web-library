---
name: ewl-migration-review
description: Operate Migration Review.md using multi-file review items. Use to open item diffs together in VS Code, locate new files in Visual Studio, approve an item, check which files are ready to stage, recognize staged approval, or record durable Migration Followup.md improvements.
---

# Interactive migration review

Typically GPT 5.6 Terra operates documents prepared by GPT-6 Astra in the original
migration session. The caller selects the model; do not change it or spawn agents.
Read repository instructions and the setup in root **Migration Review.md**, beside
the solution. Use targeted searches for item names/paths, not the whole migration
each turn. Ambiguous/missing provenance belongs with the preparation agent using
`ewl-migration-review-preparation`; do not invent mappings.

## One approval covers one review item

A **review item** owns explicitly listed changed regions across one or more files,
including deleted regions/files. `Approve`, `mark reviewed`, and equivalent explicit
decisions approve the ENTIRE established item scope. No separate removal acceptance
is needed. Supporting references, origins and callers outside that scope are NOT
approved transitively. They may be owned by other items or retained unchanged.

Each changed line and file-level change must have exactly one owner in the file
index. Distinguish baseline deleted lines from current added lines. A replacement
includes both. New/deleted files require all their added/deleted lines; empty files,
renames, modes, and binary changes require file-level ownership as well.

The file index is a coverage check, not merely a list of associated items. A file is
ready to stage only when all its changed regions and file-level changes are assigned
and approved for the current content. Do not certify from Markdown checkmarks alone.

## Helpers and disposable state

Git systems require Node.js 22+ and Git on PATH. Resolve scripts relative to THIS
loaded skill's directory. Use the harness's PowerShell tool on Windows.

```powershell
node '<skill>\scripts\review.mjs' inspect --repo '<repository root>'
```

Output reports the baseline, current file inventory, exact changed-line ranges,
coverage errors, per-item approval and file readiness. Nonzero exit means an
operational error; an exit-zero report with `errors` is NOT complete coverage.
Coverage uses the immutable baseline -> working copy, including untracked files and
already-committed migration pieces. Check that the mapped baseline is the intended
one. Unrelated changes require explicit reasoned exclusions in the document; do
not exclude something merely to get a green report. Each item's origins must state
whether they remain in use/inactive, are removed/partially removed, or are unresolved.

Approval content snapshots are stored under the OS temp directory's
`opencode/migration-review/<repository-and-baseline-key>/`, outside the repository.
The report names the location. No per-item review-notes or evidence fields in the
Markdown. A missing snapshot means unverifiable approval, not automatic restoration
from a checkmark. Ask the user before reapproving. Keep the temp state for the review
lifetime; remove it only after the user confirms completion. Never delete unrelated
temp directories.

The helper never stages, commits, updates Markdown, or edits source. The agent
updates the document's item statuses and file-readiness summary from fresh output.
Read the affected sections before edits to preserve concurrent user changes.

For Mercurial, load `ewl-mercurial`. There is no index and these helpers require Git;
use explicit decisions/manual diffs and disclose the missing machine readiness
check. Never run Git on a Mercurial working copy.

## Open an item's diffs in one VS Code window

For `start review`, select the first pending item. For `next review item`, select the
next pending item without approving the previous one. For a named file, use the file
index; if several items own it, offer those names instead of guessing.

```powershell
& '<skill>\scripts\Open-ReviewItem.ps1' -Repository '<root>' -Item '<exact item title>'
```

The launcher prepares immutable-baseline files outside the repository, then opens
ONE dedicated VS Code workspace window with a diff tab for each scoped file pair.
Preview tabs are disabled to keep all tabs visible. It omits `--wait`. Added files
compare empty -> new; deleted files compare old -> empty without recreating them in
the repository. Renames compare old -> current path. Binary content can be covered
but may need an appropriate viewer rather than a text diff.

The launcher opens the workspace with `--new-window`, waits for its unique window
title, then foregrounds that window before each `--reuse-window --diff` call. If it
cannot identify/focus the intended window, it stops rather than sending diffs to an
unrelated window. This deliberate navigation does not inspect the user's VS Code
selection. Do not switch windows during the launch batch. `-PrepareOnly` generates
the navigation plan/workspace without
launching UI and is useful for verification. Temp baseline copies are read-only in
the generated workspace; the working-copy side remains editable.

Default: fixed baseline -> working copy. `-Source index` explicitly displays fixed
baseline -> index, NOT HEAD -> index. For requests for uncommitted/staged changes
relative to HEAD, use explicit Git comparisons and separately materialized resources;
label that view and preserve the fixed-baseline ownership inventory. Do not change
the document's baseline to make a partial view fit. A partial comparison does not
by itself establish that the entire item's migration scope was reviewed.

Remember the current item and compared versions. Opening supporting legacy origins
does not replace this target. A bare `approved` refers to this established item,
not whatever unrelated editor was last selected. Echo the item's name and scope.
If the user switches manually through Fork, ask for the file/item when needed;
process arguments do not expose current diff-tab/caret state reliably.

For `open origins`, read only the item's relevant origins, materialize/open their
exact immutable baseline versions, and use `code --reuse-window --goto <file>:<line>:1`
in the review window. State the inclusive ranges and retention/duplication context.
The CLI does not highlight arbitrary ranges; do not claim it does. A Fork temporary
file labeled staged is not proof that it is the migration baseline.

## Keep Visual Studio detection for new files

When the user reviews new source in VS or asks `review this VS file`, query:

```powershell
& '<skill>\scripts\Get-VisualStudioLocation.ps1' -SolutionPath '<absolute solution path>'
```

Use Windows PowerShell 5.1 and a bounded tool timeout (normally 20 seconds). The
read-only helper matches the solution, not the foreground application; the user
has alt-tabbed to OpenCode. Require one matching `Instances` entry with
`EditorState: Text`; surface `Errors`. For duplicate solution instances, ask for
the process ID and pass `-ProcessId`.

Use selection, otherwise caret, to locate the owning item through the file index
and current source. Coordinates are 1-based; selection end is exclusive. Rectangular
selections or selections spanning items require scope clarification. If `Saved` is
false, ask the user to save before disk-based mapping/approval; never save for them.
If `CaretVisible` is false or the caret is above `FirstDisplayedLine`, ask for a
click/selection in the intended code. Null visibility means unknown. Do not infer
the last source line from display height when folding/wrapping is possible.

VS and VS Code can both retain background state. Do not silently switch between
them. Use an explicit request or the established review context.

## Approve, reopen, and detect changes

Before approving, refresh coverage. Correct simple line-coordinate drift only after
verifying source and ownership; substantive changes go to preparation. Do not
silently broaden an item to absorb new edits. No uncovered/duplicate/stale ranges
are allowed when approving.

Approval snapshots describe saved disk/index content. With no VS Code inspection
extension, unsaved diff-editor buffers cannot be verified; have the user save edits
before approval when editing in the diff. Never claim the snapshot contains an
unsaved buffer. A read-only baseline or index side must not be edited.

On explicit whole-item approval:

```powershell
node '<skill>\scripts\review.mjs' approve --repo '<root>' --item '<exact item title>'
```

Then inspect again and update the item's `Status` and file-readiness summary.
Opening a diff, moving to the next item, or recording a follow-up is not approval.
If the user approves only part, do not approve the whole item: clarify or ask
preparation to split its scope. Keep supporting-reference items untouched.

For `needs work`/`reopen`, use the helper's `reopen` command with the same arguments
and describe the specific blocker in item prose. `recompare` opens the established
item again and refreshes state, not automatic reapproval. Changed approved content
produces `Changed since approval`; preserve the old local snapshot until the user
reapproves. Missing/ambiguous ownership or changed coordinates require rechecking,
not overwriting the evidence to match. Readiness includes ALL owning items of a
file, not merely the last one reviewed.

### Recognize staging without changing it

User staging is approval of the staged portions. Inspect BOTH `git diff --cached`
and `git diff`, against HEAD for what was staged and against the fixed baseline for
full migration coverage. A staged change does not approve an unrelated earlier
committed change in the same file. Hunk context is not approval.

With valid working-copy coverage, run `review.mjs sync-staged --repo <root>` to
persist safe staged approvals. It recognizes files whose entire baseline change
is staged and whose working content matches the index. It records only those
files' owned regions, not other files or supporting references in the item. A
multi-file item remains pending until every owned region is approved; an already
fully staged file can itself be ready while other files in that item await review.
Read `approvedRegions`/`totalRegions` and `stagingDeferred` in the output. The latter
requires explicit scope verification, not a guessed approval. Staged-region
snapshots survive commits and expire naturally when their content/mapping changes.

If staging covers an entire item's scope (or completes prior verified approvals),
record approval with `approve --source index` after verifying its complete indexed
scope. The map's current-side ranges must match the index for that command. If
coordinates or partial scope do not match, keep the item pending and report exactly
what is staged; ask for explicit item approval or refreshed mapping rather than
guessing. Never alter index contents to make the command pass. Working-copy edits
after staged approval remain unapproved. Run normal working-tree inspection before
claiming a file is ready. A commit alone does not imply approval; recorded approval
survives commits, but an unobserved staging action needs user confirmation.

Never stage/unstage/reset/restage unless explicitly asked. User-owned partial-file
staging is preferred. Explicit file selection is mandatory for requested staging or
commits: do not sweep up disposable Migration Review.md. Review commits need not
build independently. Do not add source changes just to make them buildable.

## Follow-up and completion

For `follow up after migration` or equivalent, add an unchecked improvement to root
**Migration Followup.md**, with modern paths/symbols, rationale, desired outcome and
dependencies. It must stand alone after temporary review material is deleted. Do
not defer a current defect or missing port without the user's instruction. A
follow-up never changes approval or ownership. This file is intended for VCS, but
is not staged or committed automatically.

For `which files are ready to stage`, run fresh working-tree inspection and report
ready files separately from pending items, changed approvals, and coverage errors.
Mirror the result in `File readiness`; never certify from that cached table alone.
For `what is left`, also list unresolved origins/deletions and deferred follow-ups
separately. No separate legacy ledger or removal-approval state is needed.

Keep Migration Review.md unstaged; no ignore-file changes or review subdirectory.
After the user confirms migration commits and completed review, delete only the
disposable document and this review's local state. Preserve Migration Followup.md.
