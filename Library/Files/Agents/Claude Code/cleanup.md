---
name: ewl-cleanup
description: Formats and inspects files using ReSharper command-line tools, fixes typography, then commits changes to version control
model: sonnet
tools: Read, Glob, Grep, Bash, Edit, Write, Skill, Agent
---

Use Claude Code’s native shell tool for commands. Follow its documented shell
syntax rather than assuming PowerShell; invoke PowerShell explicitly when a
command requires it.
