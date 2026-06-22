---
name: ewl-agent-testing
description: Test changes to EWL agent rules, subagents, skills, OpenCode plugins, and MCP tools by driving OpenCode headlessly, capturing transcripts, and cleaning up afterwards. Load this skill when verifying changes to anything under Library\Files\Agent Rules.md, Library\Files\Agents\, Library\Files\Agent Skills\, Library\Files\OpenCode Plugins\, or Library\Files\OpenCode Tools\.
---

## Overview

Changes to agent configuration (`AGENTS.md`, subagent prompts, skills, plugins, MCP tools) only take effect in a **newly started** OpenCode session -- the TUI running in front of you does not pick up edits to these files. To verify that the change behaves correctly, drive a fresh OpenCode instance headlessly and inspect what it did.

This skill documents the end-to-end loop:

1. Make the source edit in the EWL source tree.
2. Propagate to the target test system(s) with `sync`.
3. Launch `opencode run` headlessly against each target system.
4. Capture session transcripts from flat-file storage.
5. Check logs for permission-ask events and other red flags.
6. Revert any repository changes the test produced.
7. Delete the test session via the HTTP API and clean up flat-file leftovers.

## Before you start

### Pick a target system

Never test in the EWL source repository itself. Test in a client system that references EWL, because EWL's own configuration is regenerated (not consumed) by the DU. Good targets include:

- **EWL System Manager** (`$env:USERPROFILE\Revision Control\EWL System Manager`) -- Mercurial, no sub-repos, well-populated with C# files.
- **TEWL** (`$env:USERPROFILE\Revision Control\EWL Dependencies\EnduraCode's TEWL`) -- Mercurial outer repo with a Git sub-repo at `Shared\`. Use this for sub-repo tests and for tests that exercise paths containing a curly apostrophe (U+2019).

If the test involves the repo having `.hg` at the top, note that `Test-Path -LiteralPath` is required when the path contains U+2019; `Test-Path` with a plain string will sometimes fail to match.

### Pre-flight checks

- The TUI session you are running in now will still work while tests run; `opencode run` launches a separate process with its own lifetime.
- `hg status` / `git status` in the target system should be clean or in a known state. Testing typically leaves test edits behind, which you must revert. A dirty target repo can cause the primary agent to ask a clarification question, which blocks headless testing.
- Both the primary model and the subagent model consume Zen credits (for `opencode/*` model IDs) or upstream provider credits (for `anthropic/*`, `openai/*`, etc.). Budget accordingly: expect a few dollars for a full test matrix of ~8 scenarios.

## Propagating edits to the target system

After editing any file under `Library\Files\` in the EWL source repo, run the EWL DU against the target system so its `.opencode\`, `.claude\`, and `Library\Generated Code\EWL Agent Rules.md` reflect your changes. Invoke the DU project from the target system directory:

```powershell
$ewlSource = (Get-Location).Path

# Target: EWL System Manager
Push-Location -LiteralPath "$env:USERPROFILE\Revision Control\EWL System Manager"
dotnet run --project "$ewlSource\Development Utility\Development Utility.csproj" -- sync
Pop-Location

# Target: TEWL (curly-apostrophe path, requires enumeration)
$parent = "$env:USERPROFILE\Revision Control\EWL Dependencies"
$tewlDir = (Get-ChildItem -LiteralPath $parent -Directory |
  Where-Object { $_.Name -like '*TEWL*' }).FullName
Push-Location -LiteralPath $tewlDir
dotnet run --project "$ewlSource\Development Utility\Development Utility.csproj" -- sync
Pop-Location
```

After the DU finishes, verify the generated file you care about looks right -- e.g. `<system>\.opencode\agents\ewl-cleanup.md`, `<system>\.opencode\tools\ewl-<tool>.js`, or `<system>\Library\Generated Code\EWL Agent Rules.md`. If you are iterating on the EWL source, rerun the DU between each iteration; `opencode run` reads these files at startup.

## Launching OpenCode headlessly

The CLI lives at `$env:LOCALAPPDATA\opencode\opencode-cli.exe`. The `run` subcommand takes a prompt and exits when the model stops. Important flags:

| Flag | Purpose |
|---|---|
| `--format json` | Stream raw JSON events to stdout; one JSON object per line. Captures every tool call, message part, and step boundary. |
| `--model <provider/model>` | Pick the primary model. Examples: `opencode/gpt-5.4`, `opencode/claude-sonnet-4-6`. |
| `--agent <name>` | Primary agent role. Use `build` to match the normal interactive default. |
| `--title <string>` | Human-readable session title. Very useful for filtering later. Always prefix with a scenario tag like `scen1-<what>`. |
| `--print-logs --log-level DEBUG` | Optional diagnostics when debugging hangs or permission behavior. These do not replace transcript inspection. |
| (positional) | The prompt text. |

Minimal invocation pattern:

```powershell
Push-Location -LiteralPath $targetDir
& "$env:LOCALAPPDATA\opencode\opencode-cli.exe" run `
  --format json `
  --model "opencode/gpt-5.4" `
  --agent build `
  --title "scen1-preedit-gpt54" `
  "In Library/MailMerging/GlobalFields/GlobalRowType.cs, add a summary doc comment to the class." `
  | Out-File -FilePath "$env:TEMP\ewl-agent-tests\scen1.jsonl" -Encoding utf8
Pop-Location
```

Each `opencode run` creates a fresh primary session -- no TUI restart or cache clearing needed. Agent/skill files are re-read on every startup.

There is no supported `opencode run` switch that disables interactive questions or auto-answers prompts. Prevent blocking questions by starting from a clean target repo, using prompts that are specific enough for the primary agent to act without clarification, and configuring permissions to `deny` rather than `ask` for operations that should not happen during the test.

### Prompt style

Prompts to the primary agent should describe a **real change request**, not a test-framework instruction. The agent must discover workflow rules (Critical Rules, agent-invocation order) from its `AGENTS.md` on its own. **Don't** write prompts like "invoke the ewl-cleanup subagent on file X per Critical Rule #2"; that short-circuits exactly the wiring you are trying to test. **Do** write prompts like "Add a public constant `Yaml` with value `.yaml` to Shared/Tewl/FileExtensions.cs".

For direct-invocation tests of a single subagent (bypassing the primary), either use `--agent <subagent-name>` or phrase the prompt so the primary clearly delegates (e.g. "Please run the ewl-cleanup subagent directly on this file...").

Prefer compile-safe prompts unless the scenario specifically validates behavior in the presence of compiler errors. Artificially invalid code can distract the primary agent or inspection tools, produce noisy diagnostics, and obscure the behavior under test.

### Extracting the session ID

The session ID appears in every JSON event as `sessionID`. Pull the first one:

```powershell
$content = Get-Content $jsonlPath -Raw
if ($content -match '"sessionID":"(ses_[^"]+)"') {
  $sessionId = $matches[1]
}
```

Record each test's session ID (plus target dir) to a list so you can delete them at the end.

### Detecting hangs

If the primary or a subagent loops on an issue (e.g. fighting with a curly-apostrophe path), the `opencode run` process may stop making progress but not exit. In some cases the JSONL wrapper may also fail to return even though flat-file storage shows the primary session and child sessions have already reached `step-finish` with `reason: "stop"`. Symptoms:
- `Get-Content $jsonlPath` stops growing.
- The OpenCode debug log file stops accumulating lines.
- The `opencode` process (`Get-Process -Name opencode`) keeps running for minutes with no new events.
- Flat-file storage shows no new `message\` or `part\` files for the primary session or child sessions.

When this happens, treat flat-file storage as authoritative. If the primary session and all relevant child sessions have stopped, classify the scenario from the stored transcript even if `opencode run` timed out or had to be killed. If the transcript shows an in-progress tool call, permission `ask`, question prompt, or model loop, kill the run and mark the scenario inconclusive or failed according to the test contract.

Use a shorter per-run watchdog timeout during large matrices, then inspect storage before deciding whether the scenario actually failed. Kill only processes that belong to the current test window:

```powershell
Get-Process -Name opencode -ErrorAction SilentlyContinue |
  Where-Object { $_.StartTime -gt (Get-Date).AddMinutes(-30) } |
  Stop-Process -Force
```

If the partial transcript ends before the scenario's required evidence exists, mark the scenario as inconclusive. If the partial transcript includes a final primary response and stopped child sessions, it is not automatically inconclusive; record it as pass/fail using the evidence in storage.

## Reading the transcript (flat-file storage)

`opencode run` writes session data to flat files under `%USERPROFILE%\.local\share\opencode\storage\`, **not** the SQLite database `opencode.db`. The database is used by the TUI for its own persistence; don't look there for `run` sessions.

Flat-file layout:

```
storage\
  session\
    global\ses_<id>.json           # session metadata (title, parentID, directory)
  message\
    ses_<id>\
      msg_<id>.json                # per-message metadata (role, etc.)
  part\
    msg_<id>\
      prt_<id>.json                # individual parts: tool calls, text, reasoning
  session_diff\ses_<id>.json       # internal diff tracking
```

The `prt_*.json` files carry the useful content. Each has a `type` field (`tool`, `text`, `reasoning`, `step-start`, `step-finish`). For tool calls:

```json
{
  "type": "tool",
  "tool": "bash",
  "state": {
    "status": "completed",
    "input": { "command": "hg status", "description": "..." },
    "output": "..."
  }
}
```

A PowerShell helper for walking a session + its children:

```powershell
param([Parameter(Mandatory)][string]$SessionId)

$storage = "$env:USERPROFILE\.local\share\opencode\storage"

# primary session file
$sess = Get-ChildItem "$storage\session" -Recurse -File -Filter "$SessionId.json" |
  Select-Object -First 1 | ForEach-Object { Get-Content $_.FullName -Raw | ConvertFrom-Json }

# enumerate children (subagent invocations via the task tool)
$children = Get-ChildItem "$storage\session" -Recurse -File -Filter "ses_*.json" |
  ForEach-Object { Get-Content $_.FullName -Raw | ConvertFrom-Json } |
  Where-Object { $_.parentID -eq $SessionId }

$allSids = @($SessionId) + @($children | ForEach-Object { $_.id })

foreach ($sid in $allSids) {
  $msgDir = "$storage\message\$sid"
  if (-not (Test-Path $msgDir)) { continue }
  $msgs = Get-ChildItem $msgDir -File -Filter "msg_*.json" |
    Sort-Object Name |
    ForEach-Object { Get-Content $_.FullName -Raw | ConvertFrom-Json }
  foreach ($m in $msgs) {
    $partDir = "$storage\part\$($m.id)"
    if (-not (Test-Path $partDir)) { continue }
    Get-ChildItem $partDir -File -Filter "prt_*.json" |
      Sort-Object Name |
      ForEach-Object { Get-Content $_.FullName -Raw | ConvertFrom-Json }
  }
}
```

A full version of this script -- formatting the output for quick reading -- belongs in `$env:TEMP\ewl-agent-tests\inspect-session.ps1` during a testing run.

### Child (subagent) sessions

Every call to the `task` tool creates a child session with `parentID` equal to the primary session ID and a `title` the invoking agent generated (e.g. "Format target file (@ewl-cleanup subagent)"). The child has its own `message\` and `part\` dirs. Subagent chains can go multiple levels deep; if needed, walk `parentID` recursively.

## Checking OpenCode's debug log

OpenCode writes a rotating debug log to `%USERPROFILE%\.local\share\opencode\log\<timestamp>.log`. The most useful line types for testing:

- `service=session.prompt ... sessionID=ses_...` -- session lifecycle.
- `service=llm providerID=... modelID=... sessionID=... agent=... stream` -- model calls.
- `service=permission permission=<kind> pattern=<pattern> ... action=...` -- permission decisions.
- `service=permission ... action=\"ask\" evaluated` -- blocking permission prompts. **If you see any of these for your test scenario, the agent tried to access something it shouldn't have.** During the session that led to the `external_directory: deny` rule being added to `ewl-cleanup`, these `ask` events were the exact fingerprint of the walk-up bug.

Find the log for a run window and grep for red flags:

```powershell
$log = Get-ChildItem "$env:USERPROFILE\.local\share\opencode\log\*.log" |
  Sort-Object LastWriteTime -Descending | Select-Object -First 1
Select-String -Path $log.FullName -Pattern 'external_directory.*action=\"ask\"'
Select-String -Path $log.FullName -Pattern 'hg revert|git checkout -- |git reset --hard'
```

The log does NOT capture the subagent's own reasoning text or every tool result body; for that, use the flat-file transcript above.

## Cleaning up sessions

Test sessions accumulate in OpenCode's session list and in flat-file storage. Always clean up at the end of a test run, even if the run failed (wrap the execution in try/finally semantics, or always run a manual cleanup pass before moving on).

### Step 1: Delete via HTTP API

OpenCode exposes a session-delete endpoint through `opencode serve`. The CLI does not expose `session delete` directly, but starting a local server and calling `DELETE /session/{id}?directory=<target-dir>` works cleanly.

**Critical detail**: PowerShell jobs and the Bash tool each run in their own short-lived PowerShell process. A job started in one Bash-tool invocation will not exist in the next one. Start the server with `Start-Process` (a detached OS process) and do all the delete calls within the same Bash-tool invocation:

```powershell
Add-Type -AssemblyName System.Web

$proc = Start-Process -FilePath "$env:LOCALAPPDATA\opencode\opencode-cli.exe" `
  -ArgumentList @("serve", "--port", "4099", "--hostname", "127.0.0.1") `
  -PassThru -WindowStyle Hidden

# Wait for ready
for ($i = 0; $i -lt 40; $i++) {
  Start-Sleep -Milliseconds 500
  try {
    Invoke-RestMethod -Uri "http://127.0.0.1:4099/session" -Method Get -TimeoutSec 2 |
      Out-Null
    break
  } catch {}
}

foreach ($s in $testSessions) {  # items with .id and .dir
  $encodedDir = [System.Web.HttpUtility]::UrlEncode($s.dir)
  $uri = "http://127.0.0.1:4099/session/$($s.id)?directory=$encodedDir"
  try {
    Invoke-RestMethod -Method Delete -Uri $uri -TimeoutSec 30 | Out-Null
    Write-Host "DELETED $($s.id)"
  } catch {
    Write-Host "FAILED $($s.id): $($_.Exception.Message)"
  }
}

Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
```

The `directory` query parameter must URL-encode the full absolute path, including any spaces and curly apostrophes. `System.Web.HttpUtility.UrlEncode` handles both.

### Step 2: Clean up orphan flat files

The HTTP DELETE removes the session metadata files and makes the session invisible to `opencode session list`, but it does NOT reliably delete the associated `message\ses_<id>\` and `part\msg_<id>\` directories on disk. After DELETE, explicitly purge those:

```powershell
$storage = "$env:USERPROFILE\.local\share\opencode\storage"
foreach ($id in $allTestSessionIds) {     # primary + children
  Get-ChildItem "$storage\session" -Recurse -File -Filter "$id.json" |
    Remove-Item -Force
  Remove-Item "$storage\session_diff\$id.json" -Force -ErrorAction SilentlyContinue
  $msgDir = "$storage\message\$id"
  if (Test-Path $msgDir) {
    $msgIds = Get-ChildItem $msgDir -File -Filter "msg_*.json" |
      ForEach-Object { [System.IO.Path]::GetFileNameWithoutExtension($_.Name) }
    Remove-Item $msgDir -Recurse -Force
    foreach ($mid in $msgIds) {
      Remove-Item "$storage\part\$mid" -Recurse -Force -ErrorAction SilentlyContinue
    }
  }
}
```

Child session IDs come from enumerating `session\global\ses_*.json` before deletion and collecting everything with `parentID` in the primary set. Do this enumeration BEFORE the HTTP delete runs, because DELETE removes the session JSON files first.

### Step 3: Verify cleanup

```powershell
& "$env:LOCALAPPDATA\opencode\opencode-cli.exe" session list 2>&1 |
  Select-String -Pattern "^(scen|smoke|test)" -SimpleMatch
# Expect no matches.
```

### What NOT to clean up

- **OpenCode debug log files** under `log\`. These are time-rotated and contain traffic from non-test sessions too. Leave them.
- **The SQLite database** `opencode.db`. Your `run` sessions don't write to it; the TUI does.
- **OpenCode processes that were running before the test.** They are your interactive sessions. Only kill processes you started (filter by `StartTime`).
- **Tool-output files under `tool-output\`** unless you specifically identified them as created by your test run (snapshot the directory contents before/after).

## Reverting test-induced repository changes

If the test made the primary agent actually edit files (the recommended approach) the target repo now has uncommitted changes and possibly test commits created by a cleanup subagent in commit-enabled mode. Back these out before the next scenario:

```powershell
# Mercurial target:
Push-Location -LiteralPath $targetDir
& "C:\Program Files\TortoiseHg\hg" revert --all --no-backup
# Strip any draft test commits the subagent created (e.g. "Formatted code" commits):
& "C:\Program Files\TortoiseHg\hg" --config extensions.strip= strip --no-backup <rev>
# Verify:
& "C:\Program Files\TortoiseHg\hg" log -l 3 --template "{rev}:{short(node)} {desc|firstline}\n"
Pop-Location

# Git sub-repo target (e.g. TEWL\Shared):
Push-Location -LiteralPath "$tewlDir\Shared"
git reset --hard HEAD         # only if no pre-existing uncommitted changes matter
# OR more surgically:
git reset --soft HEAD~1       # undo the test commit, keep changes staged
git reset HEAD <files>        # unstage
git checkout -- <files>       # discard
Pop-Location
```

**Important**: `hg revert` without `--no-backup` leaves `.orig` files behind. Remove them:

```powershell
Get-ChildItem $targetDir -Recurse -File -Filter "*.orig" |
  Where-Object { (Get-Date) - $_.LastWriteTime -lt [TimeSpan]::FromHours(1) } |
  Remove-Item -Force
```

**Do not use `hg strip` or `git reset --hard` on commits you didn't create in this session.** Test cleanup only targets commits/files you added during testing.

### The special `nul` file

Bash's `2>nul` (intended to suppress stderr on Windows CMD) creates an actual file called `nul` in the CWD when run under MSYS bash on Windows, because MSYS doesn't recognize `nul` as the reserved device name. Agents that don't know about this will leave a file named `nul` in the target repo. Remove it with:

```powershell
cmd /c 'del "\\?\<absolute-path>\nul"'
```

The `\\?\` prefix bypasses Win32 name validation so `del` can touch the reserved name.

## Known environment quirks

- **Paths with U+2019 (curly apostrophe)**, like `EnduraCode's TEWL`: use `-LiteralPath` everywhere, and construct paths by enumerating (`Get-ChildItem | Where-Object { $_.Name -like '*TEWL*' } | Select-ExpandProperty FullName`) rather than hard-coding the character in a literal string. MSYS bash mangles these paths; agents often fall back to `ewl-powershell` after a few bash attempts fail.
- **Post-edit plugins re-save files**, so a file's mtime may advance seconds after your `edit` tool call. If an `Edit` operation complains that a file has been modified since last read, simply `Read` it again and retry. The BOM plugin and the line-endings plugin are the common culprits.
- **Reserved Windows filenames**: `nul`, `con`, `aux`, `prn`, plus `com1`..`com9`, `lpt1`..`lpt9`. Avoid using them as file names in prompts; agents that attempt `2>nul` or similar may create them.
- **Windows PowerShell 5.1** is the default under the Bash tool. It cannot load `netstandard2.0` assemblies cleanly in some cases. When testing .NET-backed tooling, prefer a small compiled helper (`dotnet build` + `dotnet <dll>`) over `Add-Type` from PowerShell.
- **The `ewl-fix-typography` tool refuses unsupported replacements.** Since April 2026, the tool validates that the existing character at the requested position is a plausible source for the target (e.g. U+0027 for a smart-quote target). If the subagent miscomputes a column, the tool returns a refusal with context rather than silently corrupting the file. Treat a refusal as a signal to recompute the column, not to retry.

## Test matrix template

For any agent-rules / subagent / skill / plugin change, cover these five categories. Pick one or two scenarios per category for a quick smoke run; expand to all for a full verification.

| Category | What it validates | Notes |
|---|---|---|
| **Primary-agent scope** | `AGENTS.md` + Critical Rules wording steers the primary agent to invoke the right subagent at the right time | Run a real file-change prompt; inspect which subagents the primary chose to invoke. |
| **Subagent defensive checks** | The subagent refuses out-of-scope or invalid inputs | Force an out-of-scope invocation directly and verify a clean refusal (no side effects). |
| **Walk-up / permission denial** | No `external_directory` `ask` events; sessions stay inside the working directory | Grep the debug log for `action=\"ask\"` for the test window. |
| **VCS safety** | No destructive VCS ops (`hg revert`, `git reset --hard`, etc.) in the transcript | Grep the transcript's tool-call commands. |
| **Sub-repo handling** | When a sub-repo exists, subagents choose the innermost VCS for the target file | Test in TEWL (`Shared\.git` inside outer `.hg`) or any EWL system with a nested VCS dir. |

Always run each scenario at least twice before trusting the result; cheap subagent models are noticeably non-deterministic.

## End-to-end workflow summary

1. Edit the source in `Library\Files\...` of the EWL source repo.
2. From the target system directory, run `dotnet run --project "<ewl-source>\Development Utility\Development Utility.csproj" -- sync`.
3. Snapshot tool-output dir before testing (optional).
4. For each test scenario:
   a. Verify the target repo is clean except for known pre-existing files; revert previous test edits and strip only test-created commits before continuing.
   b. Stage any pre-existing setup (e.g. make a deliberate prior edit, if the scenario calls for it).
   c. `opencode run --format json --model <m> --agent build --title <scenario> <prompt>` in the target dir; capture stdout to a JSONL file.
   d. Extract the session ID from the JSONL.
   e. Record `<scenario>|<target-dir>|<session-id>` to a session-list file.
   f. Inspect the transcript via flat-file storage; note tool calls and responses.
   g. Check the debug log for `external_directory` asks and destructive VCS commands.
   h. Revert any repository changes produced during the scenario.
5. Write the test report: pass/fail per scenario with evidence.
6. Clean up (ALWAYS -- wrap in try/finally):
   a. Enumerate child session IDs from flat files before deletion.
   b. Start `opencode serve`, issue `DELETE /session/{id}?directory=<dir>` for each primary.
   c. Remove orphan `message\ses_*\` and `part\msg_*\` directories for the full ID set (primary + children).
   d. Verify with `opencode session list` -- expect no test titles.
   e. Kill the local `opencode serve` process.
   f. Remove the local test-output workspace (`$env:TEMP\ewl-agent-tests\`).
