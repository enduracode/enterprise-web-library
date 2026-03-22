$hookData = [Console]::In.ReadToEnd() | ConvertFrom-Json

$command = $hookData.tool_input.command
if( -not $command ) { exit 0 }

# Detect powershell or pwsh at a command position: start of the command string
# or after a shell separator (&&, ||, ;, |, open paren). The optional path
# prefix handles fully-qualified invocations like C:\...\powershell.exe.
# Requiring whitespace or end-of-string after the name avoids partial-word
# matches (e.g. "powershellstuff" or "powershell.bat").
if( $command -match '(?:^|&&|\|\||[;&|()])\s*(?:[\w:/\\.-]*[/\\])?(?:powershell|pwsh)(?:\.exe)?(?:\s|$)' ) {
	[Console]::Error.WriteLine(
		"Do not invoke PowerShell through the bash tool. Use the powershell MCP tool instead, " +
		"which passes commands directly to powershell.exe and avoids bash quoting issues with `$variables." )
	exit 2
}

exit 0
