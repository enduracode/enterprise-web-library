param(
	[Parameter(Mandatory)][string]$Workspace,
	[string]$Scenario = 'smoke',
	[string]$BaseUrl = '',
	[string]$UserEmail = '',
	[switch]$Headed
)

$ErrorActionPreference = 'Stop'
$Workspace = [IO.Path]::GetFullPath($Workspace)
if (-not (Test-Path -LiteralPath (Join-Path $Workspace '.ewl-web-test'))) {
	throw "Not a bootstrapped EWL web-test workspace: $Workspace"
}
$previousBrowserPath = $env:PLAYWRIGHT_BROWSERS_PATH
try {
	$env:PLAYWRIGHT_BROWSERS_PATH = Join-Path $Workspace 'browsers'
	$driverArgs = @('--scenario', $Scenario, '--artifacts', $Workspace)
	if ($BaseUrl) { $driverArgs += @('--base-url', $BaseUrl) }
	if ($UserEmail) { $driverArgs += @('--user-email', $UserEmail) }
	if ($Headed) { $driverArgs += '--headed' }
	& dotnet run --project (Join-Path $Workspace 'Driver\Driver.csproj') -- @driverArgs
	if ($LASTEXITCODE -ne 0) { throw "Web test failed. Read $Workspace\report.json and failure screenshot." }
}
finally {
	$env:PLAYWRIGHT_BROWSERS_PATH = $previousBrowserPath
}
