export const RequirePowerShellToolPlugin = async () => ({
	"tool.execute.before": async (input, output) => {
		if (input.tool !== "bash") return

		const command = output.args.command
		if (typeof command !== "string") return

		// Detect powershell or pwsh at a command position: start of the command string
		// or after a shell separator (&&, ||, ;, |, open paren). The optional path
		// prefix handles fully-qualified invocations like C:\...\powershell.exe.
		// Requiring whitespace or end-of-string after the name avoids partial-word
		// matches (e.g. "powershellstuff" or "powershell.bat").
		const pattern = /(?:^|&&|\|\||[;&|()])\s*(?:[\w:/\\.-]*[/\\])?(?:powershell|pwsh)(?:\.exe)?(?:\s|$)/i
		if (pattern.test(command)) {
			throw new Error(
				"Do not invoke PowerShell through the bash tool. Use the ewl-powershell tool instead, " +
					"which passes commands directly to powershell.exe and avoids bash quoting issues with $variables."
			)
		}
	},
})
