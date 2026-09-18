---
name: ewl-migration-review
description: Operate an existing Modern Review.md and Legacy Coverage.md during an interactive migration review. Use for open legacy origins, mark reviewed, sync staged review status, record reconciliation, show what remains, or recompare the code selected in Visual Studio.
---

# Interactive migration review

## Session setup

The user reviews/edits modern code in Visual Studio, opens legacy code in VS Code,
and normally owns staging, especially partial-file staging. This skill is a narrow
navigation/bookkeeping workflow typically run with GPT 5.6 Terra from documents
prepared by GPT-6 Astra in the original migration session. The caller chooses
the model/session; do not change models or launch another agent automatically.

1. Read repository instructions and detect the VCS in the modern repository. Load
   `ewl-mercurial` before Mercurial commands. Staging rules apply only to Git;
   Mercurial has no index and uses explicit review decisions instead.
2. Locate `Migration Review/Modern Review.md` and `Legacy Coverage.md` (or their
   established alternate location). Read setup sections and check that baseline
   revision and source location agree. The OpenCode directory need not be the
   modern repository. Resolve the solution path relative to the modern repository.
3. Use targeted searches for file/symbol headings and legacy IDs. Read only relevant
   entries and source ranges per turn, not the entire migration or both ledgers.

If documents, origins, or symbol boundaries are missing, ambiguous, or invalidated
by significant edits, flag the problem for the migration author using
`ewl-migration-review-preparation`. Never invent provenance. Use the supplied
mapping/checklist; substantive migration redesign belongs with the author.

## Query Visual Studio in the background

Use the bundled script from this loaded skill's actual directory with Windows
PowerShell 5.1, through the harness's PowerShell tool, and a bounded timeout (normally
20 seconds):

```powershell
& '<skill-directory>\scripts\Get-VisualStudioLocation.ps1' `
  -SolutionPath '<absolute solution path>'
```

The helper enumerates VS instances and matches the solution path. It reads state
without activating VS. Foreground-window detection is wrong: the user has alt-tabbed
to OpenCode. The JSON contains `Instances` and `Errors` arrays. Require one matching
instance with `EditorState: Text`. Report inaccessible/busy instances honestly; they
are not necessarily closed. For duplicate matching solutions, ask which process
ID and then pass `-ProcessId`.

- Use a nonempty selection first, otherwise the caret.
- Endpoints are 1-based with an exclusive end. Ending at column 1 excludes that
  line. Rectangular selections (`SelectionMode` other than 0) need clarification.
- Resolve current symbols/sections rather than trusting advisory modern line
  numbers. A selection spanning entries needs an explicit scope before approval.
- If `Saved` is false, ask the user to save before mapping disk lines or approving
  code. Never save their buffer yourself.
- If `CaretVisible` is false or the caret is above `FirstDisplayedLine`, ask for a
  click/selection in the intended code. A null visibility result is unknown. Do not
  infer the last visible source line from pane height: folding/wrapping changes it.

## Open origins

For `open origins`, `show legacy`, or a new location-based comparison:

1. Resolve the modern entry from VS (or an explicit user-supplied file/symbol).
2. Read that entry and its referenced legacy blocks.
3. Confirm the legacy checkout is at the pinned revision and the requested files
   have no local changes. Validate their ranges. If not, request preparation rather
   than comparing the wrong source. Never modify the legacy checkout.
4. Open a dedicated VS Code window once per session, then open origins in documented
   order at their starting lines:

```powershell
code --new-window '<legacy-checkout-directory>'
code --reuse-window --goto '<absolute-legacy-file>:<start-line>:1'
```

Quote paths and check success. A missing `code` CLI is a navigation blocker.
`--reuse-window` targets the last active VS Code window, not a named window; if the
user switches VS Code windows, do not claim the legacy window is pinned. The CLI
does not select arbitrary ranges. State the full inclusive range in chat, together
with a concise checklist and intentional differences. Open baseline source, never
a same-named modern file. Supporting context is distinct from a direct origin.

Remember this modern entry as the current review target. A later bare `mark
reviewed` refers to that established target; the user may now be viewing legacy code
or another VS document. Query VS for a new location-based request, not to silently
replace the established target. Echo the target in acknowledgments. If ambiguous,
ask. For `recompare`, reopen its origins and consult its approval evidence.

## Modern approval and legacy reconciliation are independent

Track modern review status, legacy disposition, and staging/commit state separately.
Modern code can legitimately be `Reviewed` while its origins remain `Unreconciled`.
Opening code, approving modern code, staging, and committing never imply legacy
reconciliation. Do not "fix" that discrepancy automatically.

### Recognize user staging as modern approval

Before reporting/updating modern status, inspect the index and working tree in the
modern repository, scoped to relevant mapped files (all mapped files for full status):

```text
git status --short
git diff --cached --unified=0 -- <path>
git diff --unified=0 -- <path>
```

- Fully staged new files are reviewed. For updated files, all migration changes
  staged with no newer unstaged migration edits means their modern entries are
  reviewed. Preserve earlier recorded decisions for already-committed portions.
- Partial staging approves only the staged portions. Mark a whole entry reviewed
  only when all its migration changes are approved; otherwise keep it pending and
  record the precisely approved portion. Hunk context is not approval. Resolve
  HEAD, index, and working-tree coordinates separately. Ask when ambiguous.
- Edits after staging leave the staged version approved but the affected current
  entry `Changed since review`. Do not mistake the staged file for current disk code.
- Staging Markdown, unrelated files, or generated files does not approve source
  entries. Staging mapped routing/configuration/project wiring approves those entries
  only, not behavior in other files.
- Staging NEVER changes legacy disposition or validity.

Record staged approval in `Modern Review.md`, with version-specific evidence, so it
survives a commit clearing the index. If a commit happened before staging was
observed, ask rather than inferring approval merely from committed state. Unstaging
does not erase an already-recorded approval of unchanged code.

The user owns staging. Never stage, unstage, reset, or replace index contents unless
explicitly asked. If asked to stage a whole file, inspect for extra unreviewed or
unrelated edits first. Prefer user-driven partial-file staging in VS. This includes
the Markdown documents: update their working copies, but do not restage them.

### Explicit modern approval

For `mark reviewed`, use the established target, explicit file/symbol, or (if neither
exists) VS selection/caret. Mark exactly that scope `Reviewed`, record evidence and
the user's note. A partial-section approval does not approve the whole method.
This does not stage or reconcile anything. For `needs work`, record the issue and
set `Needs work`. File checkboxes summarize entry states only.

### Evidence and subsequent edits

Resolve the entry's complete current source range and run:

```powershell
& '<skill-directory>\scripts\Get-ReviewFingerprint.ps1' `
  -Repository '<modern repository root>' -RelativePath 'Library/Example.cs' `
  -Source WorkingTree -StartLine 42 -EndLine 79
```

Store `Source`, symbol/section, inclusive range, and `Sha256` under `Approval
evidence`. For staged approval use `-Source Index`, resolving the range in index
content (`git show :<path>`), not disk lines. For full-file approval omit line
arguments and label the evidence whole-file. The helper is read-only and normalizes
BOM/newline conventions, not other whitespace. Partial approvals need precise
section/range notes and evidence; do not invent hashes.

Before relying on approval, locate the same symbol/section and recompute the hash.
Line movement alone preserves a section hash. A mismatch flags `Changed since
review` (including formatting changes); preserve previous evidence until reapproval.
For whole-file evidence, a mismatch requires checking which entries changed, not
assuming all changed. No evidence means approval freshness is unknown: ask before
claiming historical approval applies. Never silently replace a mismatching hash.

When a changed section invalidates already-reconciled legacy coverage, preserve its
prior disposition/rationale and set `Validity: Needs recheck`. This is triggered by
source changes, not by staging. Unreconciled blocks remain unreconciled. Only explicit
reconciliation confirmation clears the flag; modern reapproval alone does not.

### Explicit legacy reconciliation

Require an explicit instruction such as `reconcile this as ported`, `retain this in
the legacy API`, `intentionally remove this because ...`, or `the framework replaces
this`. Read the block's entire expected modern coverage and review notes. If only
part is accounted for, keep `Unreconciled` and record partial progress. All portions
of a split legacy block must be accounted for before reconciling the block.

Allowed dispositions: `Unreconciled`, `Ported`, `Retained/delegated`, `Intentionally
removed`, `Replaced by framework`. Non-unreconciled dispositions require the user's
rationale or explicit approval of an existing rationale. Record it and recompute
counts. Preparation's proposed disposition is not a review decision.

## Progress and edits

For `what is left`, report separately:

- Modern entries that are pending, changed, or need work, in review order.
- Unreconciled legacy blocks and reconciled blocks needing recheck; emphasize
  those with no modern counterpart.
- Invalid mappings and questions requiring the migration author.

Do not collapse these into one percentage. Before editing, reread the affected
Markdown sections and preserve concurrent user edits. Update authoritative entry
states and summary checkboxes/counts together. Report the target and changes briefly.

Do not change modern source just to simplify tracking. Do not commit automatically.
On an explicit commit request, follow repository rules and preserve index selections;
do not add unreviewed code to make a commit complete/buildable. Review commits need
not build independently. Verification of behavior remains separate from bookkeeping.
