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
$runId = (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '-' + [Guid]::NewGuid().ToString('N')
$artifacts = Join-Path $Workspace (Join-Path 'results' $runId)
New-Item -ItemType Directory -Path $artifacts -Force | Out-Null
"Run artifacts: $artifacts"
try {
	$env:PLAYWRIGHT_BROWSERS_PATH = Join-Path $Workspace 'browsers'
	$driverArgs = @('--scenario', $Scenario, '--artifacts', $artifacts)
	if ($BaseUrl) { $driverArgs += @('--base-url', $BaseUrl) }
	if ($UserEmail) { $driverArgs += @('--user-email', $UserEmail) }
	if ($Headed) { $driverArgs += '--headed' }
	& dotnet run --project (Join-Path $Workspace 'Driver\Driver.csproj') -- @driverArgs
	if ($LASTEXITCODE -ne 0) { throw "Web test failed. Read $artifacts\report.json and $artifacts\screenshots\failure.png; if the driver did not start, inspect its console output." }
}
finally {
	$env:PLAYWRIGHT_BROWSERS_PATH = $previousBrowserPath
}
