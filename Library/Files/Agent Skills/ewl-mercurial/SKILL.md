---
name: ewl-mercurial
description: Mercurial version control for EWL systems; load this skill when a .hg directory is present instead of using Git commands
---

## Overview

EWL and many of its client systems use **Mercurial** for version control. The
repositories are hosted on **Heptapod** (GitLab with Mercurial support).
When working in these repositories, use `hg` commands instead of `git`.

This skill maps common Git operations to their Mercurial equivalents and
documents EWL-specific conventions.

## Detecting the VCS

Check for a `.hg` directory at the repository root. If present, use Mercurial.
If only `.git` is present, use Git. Never mix the two in the same working copy.

## Git-to-Mercurial command mapping

| Task | Git | Mercurial |
|---|---|---|
| Status | `git status` | `hg status` |
| Diff (working copy) | `git diff` | `hg diff` |
| Diff (staged) | `git diff --cached` | *(no staging area; see below)* |
| Log (recent) | `git log --oneline -10` | `hg log -l 10 --template "{rev}:{short(node)} {desc\|firstline}\n"` |
| Log (with graph) | `git log --oneline --graph` | `hg log --graph --template "{rev}:{short(node)} {desc\|firstline}\n"` |
| Commit all changes | `git add -A && git commit -m "msg"` | `hg commit -m "msg"` |
| Commit specific files | `git add file && git commit` | `hg commit file1 file2 -m "msg"` |
| Amend last commit | `git commit --amend` | `hg amend` (requires Evolve) |
| Revert a file | `git checkout -- file` | `hg revert file` |
| Revert all files | `git checkout -- .` | `hg revert --all` |
| Pull remote changes | `git fetch` | `hg pull` |
| Update to latest | `git pull` | `hg pull -u` |
| Update to revision | `git checkout <ref>` | `hg update <rev>` |
| Show current revision | `git rev-parse HEAD` | `hg identify` |
| Show current branch | `git branch --show-current` | `hg branch` |
| Push | `git push` | `hg push` |
| Stash changes | `git stash` | `hg shelve` |
| Pop stash | `git stash pop` | `hg unshelve` |
| Create a tag | `git tag <name>` | `hg tag <name>` |
| Merge | `git merge <branch>` | `hg merge <rev>` |
| Rebase | `git rebase <base>` | `hg rebase -d <dest>` (requires rebase extension) |
| Blame/annotate | `git blame file` | `hg annotate file` |

## Key differences from Git

### No staging area

Mercurial has no index/staging area. `hg commit` commits all modified tracked
files by default. To commit only specific files, list them explicitly:

```shell
hg commit file1.cs file2.cs -m "Fix widget layout"
```

To exclude files, use the `-X` flag:

```shell
hg commit -X "*.generated.cs" -m "Manual changes only"
```

### Topics instead of branches

EWL repositories use **topics** (a Heptapod/Evolve feature) for feature work
instead of named branches or bookmarks. All work happens on the `default`
branch; topics provide a lightweight, temporary label for in-progress
changesets (analogous to Git feature branches). A topic is automatically
cleared when its changesets are published (merged to the public phase).

```shell
# Start a new topic
hg topic my-feature
hg commit -m "Add feature"

# See the current topic
hg topic

# List all active topics
hg topics

# Switch to an existing topic
hg update my-feature

# Push the current topic
hg push
```

Do not use `hg branch` to create named branches (they are permanent and
cannot be deleted). Do not use bookmarks either; topics are the standard
workflow.

### Evolve extension and safe history rewriting

EWL repositories use the **Evolve** extension, which enables safe history
rewriting with obsolescence markers. Key commands:

- `hg amend` -- amend the current commit (like `git commit --amend`)
- `hg fold` -- combine multiple sequential commits into one
- `hg prune` -- mark a changeset as obsolete (like deleting a commit)
- `hg evolve` -- automatically resolve unstable changesets after rewriting
- `hg next` / `hg prev` -- navigate between changesets in a stack

Obsoleted changesets are hidden but not destroyed. Use `hg log --hidden` to
see them if needed.

## EWL conventions

### Repository hosting and clone model

Repositories are hosted on **Heptapod** (`heptapod.host`). Heptapod uses a
multi-clone model: each developer has a named server-side clone, and there is
a `canonical` clone that serves as the authoritative repository. Remote paths
are typically configured as:

```ini
[paths]
default = https://heptapod.host/enduracode/<project>/<developer-name>
canonical = https://heptapod.host/enduracode/<project>/canonical
```

To push to your clone: `hg push` (uses `default`).
To pull from canonical: `hg pull canonical`.

### Branch and topic conventions

All development happens on the **`default`** branch (Mercurial's main branch).
Feature work uses **topics** on `default`.

### Build integration

The EWL Development Utility captures the current changeset hash for build
tagging using:

```shell
hg --debug identify --id
```

This is invoked automatically during the export/build process. The TortoiseHg
installation is expected at `C:\Program Files\TortoiseHg\hg`.

### Commit messages

Keep all lines in commit messages to **80 characters or fewer** (the
TortoiseHg default `summarylen`).

### Ignore file

The `.hgignore` file is **auto-generated** by the Development Utility. Do not
edit it manually. It uses `syntax: glob` and `subinclude` directives to
incorporate additional ignore patterns from nested `.gitignore` files:

```
syntax: glob
subinclude:.opencode/.gitignore
```
