import { basename } from "node:path"

export async function registerShellEncoding(ctx) {
  await ctx.shell.hook("create.before", (event) => {
    if (process.platform !== "win32" || !/^(pwsh|powershell)(\.exe)?$/i.test(basename(event.shell))) return
    // This hook runs before the native tool's command scan and permission evaluation.
    // Initialize before ANY output: PowerShell can cache its first output writer's encoding.
    event.command = "$OutputEncoding = [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)\n" + event.command
  })
}
