---
name: ewl-agent-testing
description: Test changes to EWL agent rules, subagents, skills, and OpenCode plugins by driving OpenCode headlessly, capturing transcripts, and cleaning up afterwards. Load this skill when verifying changes to anything under Library\Files\Agent Rules.md, Library\Files\Agents\, Library\Files\Agent Skills\, or Library\Files\OpenCode Plugins\.
---

## Overview

Use `run.mjs` against the existing authenticated dedicated server. Never start an
additional server, change global configuration, or automatically approve requests.
Run commands through the native PowerShell 7 `shell` tool; the EWL plugin initializes
UTF-8 output. Preserve personal browser isolation and existing working-copy changes.

The runner creates a fresh session, records ownership before submitting the prompt,
waits for idle, and exports the primary and descendant transcripts through the API.
Completion means the session became idle, not that the scenario passed. Inspect the
actual tool calls, permissions, outcomes, and resulting files or processes.

## Prepare a scenario

1. Choose a client system such as EWL System Manager for integration acceptance.
   Do not use the EWL source repository as a substitute for testing distributed
   integrations. Isolated tool/model probes may use a disposable workspace.
2. Verify the existing server identity and endpoint, and select an available model
   through the model catalog. Do not substitute a different provider/model silently.
3. Check applicable target-system rules and record existing version-control status.
   Prefer disposable fixtures; do not overwrite existing changes.
4. After maintained integration changes, regenerate the target with a DU containing
   those changes unless generation is explicitly deferred or waived. Inspect the
   generated files and active plugin list. Local plugin targets are package
   directories with an entrypoint. Configuration watchers can reload locations;
   session creation alone does not establish that registration has completed.
5. Create an authorized temporary output workspace and a UTF-8 prompt file. Use
   the dedicated server account’s workspace, not the personal account’s temp path.
   Never include passwords or provider credentials in prompts or artifacts.

For workflow tests, describe the real change request and let the agent discover
the applicable workflow. Do not tell it which subagents to invoke unless testing
that specific delegation. For a focused cleanup-agent test, explicitly request
delegation and verify the actual child transcript; a primary agent performing the
steps itself does not establish subagent behavior.

Prefer compile-safe fixtures. Throwaway test scaffolding does not require the
normal formatting/review workflow. Tests consume provider credits.

## Run and export

Node 22 or newer is required; the runner uses built-in modules only.

```powershell
node "<skill directory>\run.mjs" run `
  --server "http://127.0.0.1:45278" `
  --connection-file "<dedicated profile>\.config\opencode\service.json" `
  --dir "<target system directory>" --model "<verified provider/model>" `
  --title "<scenario>" --output "<managed temporary workspace>" `
  --prompt-file "<absolute UTF-8 prompt file>" --timeout-ms 300000
```

Use the actual server endpoint. The private connection file contains `password`;
the runner does not print or store its value in artifacts. Use an explicit numeric
loopback address supported by the runner rather than relying on hostname resolution.

The runner prints a manifest path in a unique `agent-test-<UUID>` directory.
Keep the manifest: it records the owned sessions and connection-file path for later
export and cleanup. Do not edit it to include unrelated sessions.

```powershell
node "<skill directory>\run.mjs" export --manifest "<manifest.json>"
node "<skill directory>\run.mjs" cleanup --manifest "<manifest.json>"
```

A timeout interrupts the primary session. Inspect descendants and use cleanup
before removing artifacts. An approval request can leave a headless test waiting;
do not bypass it. Classify the test as blocked/inconclusive when required evidence
is missing. A permission denial must not be retried through another tool.

## Evaluate results

- Inspect exported tool inputs, results, permissions, and final outcomes—not only
  the agent’s summary or the runner’s exit code.
- Confirm expected delegation and the child’s model/agent identity when relevant.
- Inspect file diffs and any reported build, browser, or process results.
- For Unicode tests, inspect code points on disk and literal characters in tool
  inputs. Typography uses ordinary edits; preserve syntax delimiters.
- Distinguish existing grants, new approval requests, and genuine denials. The
  cleanup plugin preserves existing grants but denies new requests for cleanup.
- Repeat behavioral scenarios at least twice before relying on a result. Do not
  rerun unrelated checks after success unless changes or unresolved concerns justify it.

Use the running server’s log for additional diagnostics, filtering to owned
sessions and the test window. Do not dump credentials or unrelated model payloads.

## Cleanup

Always clean up owned test sessions, including failed runs. The runner verifies
ownership before deleting through the server API; it never starts or stops the
shared server. Refresh exports before cleanup when additional evidence is needed.

Do not edit the session database or delete shared log/storage directories. Do not
use process-name-wide termination. Stop only processes demonstrably created by the
test. Remove only owned fixtures and temporary reports; preserve useful transcripts
until reviewed. Never use blanket VCS revert/reset/clean/strip commands to remove
test changes from a working repository. If recovery would affect existing work,
stop and report rather than guessing.

## Regression tests for the runner

```powershell
node --test "<skill directory>\run.test.mjs"
```

These tests use a disposable fake API. They do not establish compatibility with the
real server: also exercise a fresh authenticated session, inspect its transcript,
and verify cleanup when changing runner/API behavior.

## Workflow coverage

For changes to agent rules, subagents, skills, or plugins, select relevant cases:

| Area | Evidence |
|---|---|
| Primary-agent workflow | A realistic request discovers and follows the required workflow. |
| Subagent scope | Exact-file scope and target-system boundaries are respected. |
| Permissions | Existing grants work; new requests and genuine denials behave as intended. |
| VCS safety | No unauthorized commits, destructive recovery, or pushes. |
| Repository detection | Innermost repository is selected without probing above the target ceiling. |
| Plugin behavior | Active registration plus actual hook effects, not just source inspection. |

For cleanup scope/permission changes, additionally cover normal same-system work,
authorized cross-system work, remembered approvals, a genuine denied fixture,
standalone formatter scaffolding, nested/enclosing repositories, and exact-file
commits when that behavior changes. Supply the target directory, exact file list,
cleanup mode, applicable rules, and explicit commit authorization to the child.
Use disposable repositories for commit/recovery scenarios.

Interactive remembered-approval tests require the human to grant access through
the client. Keep them separate from unattended tests. Record unexecuted scenarios
as unverified rather than inferring success from parent access.

## Practical notes

- Use `-LiteralPath` for PowerShell file operations on paths containing apostrophes.
- Post-edit normalization can change modification times. Re-read after a tool
  reports that a file changed, and preserve the formatter’s trailing-newline state.
- Avoid shell-specific redirection such as `2>nul` under an incompatible shell;
  it can create a literal file rather than discard output.
- If an assertion fails, report the actual result and retain diagnostic evidence.
  Do not turn a workaround into a claimed pass of the original scenario.
