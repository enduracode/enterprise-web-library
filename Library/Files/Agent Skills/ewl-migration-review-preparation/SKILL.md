---
name: ewl-migration-review-preparation
description: Prepare or refresh Modern Review.md and Legacy Coverage.md for a large legacy-to-modern EWL migration. Use when the migration author needs a detailed handoff that lets a lower-cost review agent trace each modern file to exact legacy source ranges and audit leftover legacy behavior.
---

# Migration review preparation

## Purpose

Create a durable, system-specific review package for a migration that is too
large to review as one diff. Organize the main walkthrough around modern code in
source order, while independently inventorying legacy code so omissions remain
visible. Write enough explicit context that a less-capable agent can navigate and
record review decisions without rediscovering the migration's design.

This skill prepares review material; it does not approve modern code or reconcile
legacy code. The migration author may know that a mapping is likely or intentional,
but only the user's later review changes review status.

Typically run this skill in the original GPT-6 Astra migration session; the
interactive review session typically uses GPT 5.6 Terra. Use the migration author's
existing context, but verify it against actual source.
Do not select a model or launch another agent automatically. The caller chooses the
model/session. The companion `ewl-migration-review` skill owns interactive review.

## Outputs

Unless the repository already establishes another location, create these tracked
files under `Migration Review/` at the repository root:

- `Modern Review.md`: modern files in the order the user should review them,
  with entries in source order within each file.
- `Legacy Coverage.md`: an independent inventory of legacy behavior in scope,
  including behavior for which no modern counterpart has been found.

Do not create identifiers for modern entries. Identify them by relative file path
and stable symbol or section name. Give legacy blocks stable IDs such as `LEG-001`
because one modern entry may cover part or all of several legacy blocks.

## Establish the review boundary

Before writing mappings:

1. Read repository instructions and determine the version-control system.
2. Record the modern repository root and solution path.
3. Pin an immutable legacy baseline revision. Do not use a moving name such as
   `HEAD`, a branch, or a topic as the stored baseline.
4. Determine the legacy source location. Prefer a read-only worktree or separate
   checkout at the baseline. If source will be read through VCS commands instead,
   say so explicitly.
5. Define migration scope from the actual modern diff and the legacy feature area.
   Include deleted legacy pages, markup, code-behind, business logic, routing,
   configuration, project wiring, and relevant unchanged helpers.
6. Separate unrelated working-copy changes from migration scope. List exclusions;
   do not silently omit them.

Inspect staged, unstaged, and untracked changes; Git diff alone omits new untracked
files. Record the exact set of migration paths so that committing reviewed pieces
does not make them disappear from subsequent inventories. Detect Git versus
Mercurial in the target repository; load `ewl-mercurial` before Mercurial commands.
No commits, shelves, resets, or reorganization of the user's migration are needed.
Ask before creating a backup commit or a new baseline checkout. A preparation
request does not authorize staging, committing, or changing application code.

Ask one focused question if the baseline or scope cannot be established safely.

## Investigate in both directions

### Modern-first map

Inventory every new or updated non-generated source file in migration scope. Put
files in a deliberate sequential review order, normally dependencies and shared
models first, then business logic, then UI and wiring. Within each file, divide the
review into methods or meaningful contiguous sections. Avoid entries so broad that
the user cannot compare behavior, and avoid mechanical line-by-line entries.

For each entry, inspect both implementations and document:

- Exact modern symbol or section boundaries.
- Every legacy origin, with legacy ID, relative path, symbol/section, and inclusive
  baseline line range.
- Whether each origin is complete or only a specified part of the legacy block.
- How behavior moved, split, merged, delegated, or was rewritten.
- A concrete comparison checklist covering conditions, authorization, validation,
  persistence, file operations, external effects, errors, and UI behavior where
  applicable.
- Intentional differences and their rationale.
- Dependencies on other modern review entries.
- Any uncertainty requiring the migration author's attention.

Include supporting old callers, helpers, markup, stored procedures, or framework
behavior when they are necessary to understand the origin. Distinguish direct
origins from supporting context. Give the reviewer narrowly relevant ranges, not
entire files when only a few methods matter. Include full signatures for overloads
and a unique text anchor for unnamed sections.

Do not infer provenance from similar names alone. Read the implementations.
Distinguish genuinely new behavior from behavior whose origin has not been found.

### Independent legacy inventory

Build `Legacy Coverage.md` from the legacy scope itself, not by reversing the
modern map. Read all in-scope legacy files and divide them into meaningful blocks.
This independent pass is what exposes missing modern counterparts.

Include markup and declarative behavior when it affects validation, visibility,
post-backs, authorization, or navigation. Group generated designer declarations
and purely mechanical project wiring when individual review adds no value, but
record what the group contains. Include unchanged legacy code that remains in the
execution path and identify it as retained or delegated rather than pretending it
was ported.

Every legacy block starts `Unreconciled`. A known likely disposition or modern
counterpart is documentation, not approval.

## Required document format

Keep the files easy for both humans and agents to edit. Use the following structure
and field names consistently.

### `Modern Review.md`

```markdown
# Modern Review

## Setup

- Solution: `IAEM Certification Portal.sln`
- Modern repository: `<absolute path used for VS-instance matching>`
- Legacy baseline: `<full immutable revision>`
- Legacy source: `<absolute baseline checkout/worktree path>`
- Legacy coverage: [Legacy Coverage.md](Legacy%20Coverage.md)
- Scope: ...
- Excluded working-copy changes: ...

## Review Order

- [ ] `Library/Example.cs`
- [ ] `Web App/Example.cs`

## `Library/Example.cs`

### `CreateExampleAsync`

- Status: Pending
- Modern range: `CreateExampleAsync` (current lines 42-79; lines are advisory)
- Legacy origins:
  - `LEG-004`: `Legacy/Example.aspx.cs`, `createExample`, baseline lines 91-138,
    complete block
  - `LEG-009`: `Legacy/Rules.cs`, `canCreate`, baseline lines 20-41, conditions
    on lines 27-38 only
- Mapping: ...
- Compare:
  - [ ] ...
- Intentional differences: ...
- Dependencies: ...
- Preparation uncertainty: None
- Review notes: None
- Approval evidence: None
```

Allowed modern statuses are `Pending`, `Reviewed`, `Changed since review`, and
`Needs work`. A file-level checkbox is a summary only; entry statuses are
authoritative. Preserve notes and statuses when refreshing the map. If code changed
enough to invalidate reviewed provenance, set `Changed since review` and explain
why; never silently reset or retain approval.

Approval evidence is recorded by the review skill: working-tree or staged-index
version, symbol/section, and a content fingerprint from its bundled helper. It
allows approval to survive commits and line movement while detecting later edits.

For non-code files, use stable headings or descriptions instead of symbols. Current
modern line numbers are advisory because edits move them; symbols and sections are
the primary locators. Legacy ranges are fixed against the immutable baseline and
must be exact.

### `Legacy Coverage.md`

```markdown
# Legacy Coverage

## Setup

- Legacy baseline: `<same full revision as Modern Review.md>`
- Legacy source: `<same baseline checkout/worktree>`
- Scope: ...

## Summary

- Unreconciled: 12
- Ported: 0
- Retained/delegated: 0
- Intentionally removed: 0
- Replaced by framework: 0

## `LEG-004` - Candidate application creation

- Location: `Legacy/Example.aspx.cs`, `createExample`, baseline lines 91-138
- Behavior: ...
- Expected modern coverage:
  - `Library/Example.cs` - `CreateExampleAsync`: database creation and initial
    values
  - `Web App/Example.cs` - `getContent`: user interaction and validation message
- Disposition: Unreconciled
- Reconciliation notes: None
- Validity: Current
```

Allowed dispositions are `Unreconciled`, `Ported`, `Retained/delegated`,
`Intentionally removed`, and `Replaced by framework`. Except for `Unreconciled`,
every disposition requires a user-approved rationale. A block split across modern
entries remains unreconciled until all applicable parts are explicitly reconciled.
Use `Expected disposition` for the author's proposed classification; keep it
separate from the authoritative `Disposition`. `Validity` becomes `Needs recheck`
when later edits invalidate an existing reconciliation. Preserve the prior decision
and notes; the review skill handles its explicit reconfirmation.

## Refreshing existing documents

Treat existing human decisions as data:

- Preserve review notes, statuses, dispositions, and rationales unless current code
  proves they are stale.
- Add new mappings without renumbering legacy IDs.
- Mark removed or substantially changed reviewed modern entries as changed; do not
  delete their history silently.
- Add newly discovered legacy blocks with new IDs.
- Recompute summary counts and file-level checkboxes from entry-level state.
- Validate that both documents name the same baseline and source location.

Do not consume staging approval while preparing or refreshing provenance. Leave
that synchronization to `ewl-migration-review`: user staging DOES approve the staged
modern code, but never reconciles legacy blocks. If noting existing staging in the
documents, write `Staged approval not yet synchronized by the review skill`, not
`Staging is not review evidence`. Preserve approvals already recorded by review.

## Completion checks

Before reporting preparation complete:

1. Every in-scope new or updated modern source file appears in `Modern Review.md`.
2. Every modern entry has exact legacy origins or an explicit `Genuinely new` or
   `Origin unresolved` statement.
3. Every in-scope legacy block appears independently in `Legacy Coverage.md`.
4. Every cross-reference resolves in both directions, including partial coverage.
5. Legacy line ranges match the pinned baseline.
6. Both setup sections agree.
7. All new review decisions remain pending/unreconciled; author knowledge is
   recorded only as mapping, rationale, or uncertainty.

Explain any leftover legacy source ranges (including grouped mechanical content)
and any unmapped modern sections. A mapping list derived only from known ports is
not proof of exhaustive coverage. Traceability also does not prove behavior; the
documents should list verification still needed, without requiring each eventual
review commit to build independently.

Report unresolved mappings and possible omissions prominently. Do not hide them in
a general summary.
