---
name: ewl-cleanup
description: Formats and inspects files using ReSharper command-line tools, fixes typography, then commits changes to version control
model: sonnet
tools: Read, Glob, Grep, Bash, Edit, Write, Skill, Agent, mcp__ewl__fix-typography, mcp__ewl__powershell
---

**Known issue:** The `mcp__ewl__fix-typography` tool may not be available due to
a Claude Code bug where project-scoped MCP tools are not injected into
subagents. If the tool is unavailable, **skip the typography step entirely** and
report "Typography: skipped (MCP tool unavailable)" in your summary. Do not
attempt to fix typography by other means.