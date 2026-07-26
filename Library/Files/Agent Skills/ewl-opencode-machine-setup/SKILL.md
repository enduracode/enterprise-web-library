---
name: ewl-opencode-machine-setup
description: Configure opencode on a new development machine for EWL work by creating the global AGENTS.md and granting access to the EWL source, EWL System Manager source, EWL folder, and EWL client systems directory. Use when setting up opencode on a new machine, or when the user asks to bootstrap, initialize, or configure global opencode for EWL.
---

## Overview

This skill bootstraps opencode’s **global** configuration on a new Windows
development machine for Enterprise Web Library (EWL) work. It creates (or
updates) two files in the user’s global opencode config directory:

1. `AGENTS.md` -- a short instruction block listing the key EWL locations on
   the machine and how to find other EWL codebases.
2. `opencode.jsonc` -- an `external_directory` permission map granting
   access to those locations.

Both files apply to **every** opencode session on the machine, regardless of
which project directory the session is started in.

## Prerequisites and platform

This skill targets **Windows**. The default search heuristics and the example
paths below all assume Windows. If the current machine is not Windows, ask
the user for the equivalent locations rather than guessing; otherwise the
remaining steps apply.

## Step 1: Locate the global opencode config directory

opencode’s global config lives at `~/.config/opencode/`, **not** `~/.opencode/`.
On Windows that resolves to:

```
%USERPROFILE%\.config\opencode\
```

Create the directory if it does not already exist.

## Step 2: Find the EWL locations

You need to resolve the following four absolute paths on the current
machine:

| Label | Typical Windows location | What’s at this path |
|---|---|---|
| EWL source code | `%USERPROFILE%\Revision Control\EwlBill` | Contains `Enterprise Web Library.sln`, `Core\`, `Library\`, `Development Utility\` |
| EWL System Manager source code | `%USERPROFILE%\Revision Control\EWL System Manager` | Contains `Installation Support Utility\`, `Web Site\`, `Program Runner\`, `Data Cleaner\` |
| EWL folder | `C:\Enterprise Web Library` | Machine-wide EWL infrastructure: `Local NuGet Feed\`, configuration, etc. |
| EWL client systems directory | User-provided | Contains EWL client codebases; each codebase can be identified by a `dotnet-tools.json` file containing `ewl` |

For each path, resolve it using the following procedure:

1. **Try the default location** from the table above. For the EWL client
   systems directory, ask the user for the location with the `question` tool
   instead of assuming a default.
2. **If not found, search common alternates.** Probe each of these as a
   possible parent for the EWL source and EWL System Manager directories:
   - `D:\Revision Control\`
   - `C:\Source\`
   - `C:\Repos\`
   - `C:\dev\`
   - directly under `%USERPROFILE%\` (i.e., `%USERPROFILE%\EwlBill`, etc.)

    For the EWL folder, probe `D:\Enterprise Web Library` as the most likely
    alternate.
3. **Sanity-check candidates** by looking for the expected marker files /
   subdirectories from the table above before committing to a match.
4. **If still not found, ask the user** with the `question` tool. Allow the
   user to skip a path that doesn’t exist on this machine (e.g., a developer
   may only have EWL source and not the EWL System Manager). Skipped paths
   are simply omitted from the generated `external_directory` map.
5. **Verify each user-provided path exists** with `Test-Path` before writing
   it into config.

Store the four resolved absolute paths (or note which ones the user
skipped) for use in the next steps.

## Step 3: Read existing files at the target locations

Before writing anything, read whatever may already be there so you can merge
instead of clobber:

- `%USERPROFILE%\.config\opencode\AGENTS.md`
- `%USERPROFILE%\.config\opencode\opencode.jsonc`
- `%USERPROFILE%\.config\opencode\opencode.json` (alternate filename --
  if this exists instead of `.jsonc`, work with it; do not create both)

If either file contains content unrelated to this skill, **do not overwrite
it**. Surface the existing content to the user and ask how to proceed
(merge, replace, or abort).

## Step 4: Write `AGENTS.md`

Target path: `%USERPROFILE%\.config\opencode\AGENTS.md`

Use **concrete absolute paths** (the values resolved in Step 2), not
templated `~/...` or `%USERPROFILE%\...` references. The file is read as
prose by the model, and literal paths are clearer.

Template (substitute the resolved paths; omit any line for a skipped path):

```markdown
# Global Agent Rules

If looking for something possibly involving the Enterprise Web Library (EWL), check these locations:

- EWL source code: `<resolved EWL source path>`
- EWL System Manager source code: `<resolved ESM path>`
- EWL folder: `<resolved EWL folder path>`
- EWL client systems: `<resolved EWL client systems path>`. When the user asks how other EWL systems do something, or which systems implement an EWL feature, search this directory recursively for `dotnet-tools.json` files containing `ewl` and inspect the matching codebases.
```

If a previous version of this block is already in the file, replace it with
the freshly resolved version. If the file has unrelated content, merge by
appending the block at the end (with a blank line separator) after
confirming with the user.

## Step 5: Write `opencode.jsonc`

Target path: `%USERPROFILE%\.config\opencode\opencode.jsonc`

If a `.json` form already exists instead, edit that file in place and keep
the `.json` extension. Otherwise create `.jsonc`.

### Pattern format

Paths in `external_directory` patterns must use **forward slashes**, even
on Windows. opencode normalizes path separators internally before matching,
and forward-slash globs are the documented convention.

Append `/**` to each path so the rule matches all descendants.

Example (with the standard locations and a user-provided EWL client systems
path):

```jsonc
{
  "$schema": "https://opencode.ai/config.json",
  "permission": {
    "external_directory": {
      "C:/Users/willi/Revision Control/EwlBill/**": "allow",
      "C:/Users/willi/Revision Control/EWL System Manager/**": "allow",
      "C:/Enterprise Web Library/**": "allow",
      "<resolved EWL client systems path with forward slashes>/**": "allow"
    }
  }
}
```

### Merge rules when the file already exists

- Preserve `$schema` and any existing fields the user did not ask to change.
- If `permission.external_directory` already exists as a map, add the EWL
  entries, including the EWL client systems directory, to the existing map.
- If a key for one of our paths already exists with a different action,
  ask the user before overwriting.
- **Do not** add a `"*": "ask"` fallback. opencode’s built-in default for
  `external_directory` is already `"ask"`; an explicit `"*": "ask"` entry
  is redundant and (because the pattern map is evaluated in insertion order
  with first-match-wins for `external_directory`) could shadow later
  entries if placed at the top.

### What this grants

opencode’s `external_directory` permission gates any tool call that touches
paths outside the current working directory: `read`, `edit`, `write`,
`list`, `glob`, `grep`, and many `bash` commands. Adding an `allow` entry
for a path means tool calls under that path proceed silently. Because
`read` and the editing tools all funnel through this gate, a single
`external_directory` allow per path is sufficient for full read/write
access.

## Step 6: Validate

If you are uncertain about any config field shape, fetch the authoritative
schema at <https://opencode.ai/config.json> and verify before writing.
opencode validates strictly on startup and refuses to start when config is
invalid, so a broken file means a broken opencode.

If the user already has a broken global config and opencode will not start,
the escape hatches are:

- Start opencode from a project directory with
  `OPENCODE_DISABLE_PROJECT_CONFIG=1` to skip project config.
- Set `OPENCODE_CONFIG_CONTENT='{"$schema":"https://opencode.ai/config.json"}'`
  to inject inline JSON as a final merge.

## Step 7: Tell the user to restart opencode

opencode loads config and `AGENTS.md` **once at startup**; there is no
hot-reload. After saving, instruct the user to quit and restart opencode
for the changes to take effect.

## Step 8: Verification (after restart)

To confirm the setup is working, the user can ask opencode (in any session)
to read a file under one of the configured paths, or to list the EWL client
systems on the machine. If the read or search proceeds with no
`external_directory` prompt, the rules are active. The global `AGENTS.md` will
also be visible: it is injected at the top of the system prompt of every
session as `Instructions from:
C:\Users\<name>\.config\opencode\AGENTS.md` followed by the file contents.
