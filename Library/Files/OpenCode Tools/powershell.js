import { tool } from "@opencode-ai/plugin"
import { spawn } from "child_process"

const MAX_OUTPUT = 51200

export default tool({
  description:
    "Execute a PowerShell command or script. Always use this tool instead of " +
    "running PowerShell through the bash tool. The command is passed directly " +
    "to powershell.exe with -NonInteractive and -NoProfile, avoiding bash " +
    "quoting issues (e.g. $variables being stripped) and interactive prompts.",
  args: {
    command: tool.schema
      .string()
      .describe(
        "The PowerShell command or script to execute. Write normal PowerShell " +
          "syntax; $variables, quotes, and Unicode paths work as expected.",
      ),
    workdir: tool.schema
      .string()
      .optional()
      .describe("Working directory. Defaults to the project root."),
    timeout: tool.schema
      .number()
      .int()
      .min(1000)
      .optional()
      .describe("Timeout in milliseconds. Defaults to 120000 (2 minutes)."),
  },
  async execute(args) {
    const timeoutMs = args.timeout ?? 120000

    return new Promise((resolve) => {
      let done = false
      const finish = (result) => {
        if (done) return
        done = true
        clearTimeout(timer)
        resolve(result)
      }

      const script =
        "$ProgressPreference = 'SilentlyContinue'; " +
        "[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; " +
        args.command
      const encoded = Buffer.from(script, "utf16le").toString("base64")

      const proc = spawn(
        "powershell.exe",
        ["-NonInteractive", "-NoProfile", "-EncodedCommand", encoded],
        {
          cwd: args.workdir || undefined,
          stdio: ["ignore", "pipe", "pipe"],
          windowsHide: true,
        },
      )

      let stdout = ""
      let stderr = ""
      let stdoutTruncated = false
      let stderrTruncated = false
      proc.stdout.on("data", (chunk) => {
        if (stdoutTruncated) return
        stdout += chunk.toString()
        if (stdout.length > MAX_OUTPUT) {
          stdout = stdout.slice(0, MAX_OUTPUT)
          stdoutTruncated = true
        }
      })
      proc.stderr.on("data", (chunk) => {
        if (stderrTruncated) return
        stderr += chunk.toString()
        if (stderr.length > MAX_OUTPUT) {
          stderr = stderr.slice(0, MAX_OUTPUT)
          stderrTruncated = true
        }
      })

      const timer = setTimeout(() => {
        spawn("taskkill", ["/pid", String(proc.pid), "/f", "/t"], {
          stdio: "ignore",
          windowsHide: true,
        })
        finish(
          stdout.trim() +
            (stderr ? "\n\nStderr:\n" + stderr : "") +
            "\n\nProcess timed out after " + timeoutMs + "ms and was killed.",
        )
      }, timeoutMs)

      const finishWithCode = (code) => {
        const suffix =
          (stderr ? "\n\nStderr:\n" + stderr : "") +
          (stdoutTruncated || stderrTruncated
            ? "\n\n(Output truncated at " + MAX_OUTPUT + " bytes)"
            : "") +
          (code !== 0 ? "\n\nProcess exited with code " + code + "." : "")
        finish(stdout.trim() + suffix)
      }

      // Resolve on "exit" (the PowerShell process terminating), not "close".
      // "close" additionally waits for stdout/stderr to reach EOF, which never
      // happens when the command launches a detached, long-lived process (e.g. a
      // web server) that inherits those pipe handles -- that would wedge the tool
      // until the timeout. After "exit" we allow a short grace window for any
      // buffered output to flush, then resolve. If "close" fires first (the
      // common, no-detached-child case), we resolve immediately with no delay.
      let closed = false
      proc.on("close", (code) => {
        closed = true
        finishWithCode(code)
      })
      proc.on("exit", (code) => {
        if (closed) return
        proc.stdout.unref?.()
        proc.stderr.unref?.()
        setTimeout(() => finishWithCode(code ?? 0), 200)
      })

      proc.on("error", (err) => {
        finish("Failed to start powershell.exe: " + err.message)
      })
    })
  },
})
