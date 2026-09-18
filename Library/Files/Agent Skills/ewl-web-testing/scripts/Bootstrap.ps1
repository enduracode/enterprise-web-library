param(
	[string]$Workspace = ''
)

$ErrorActionPreference = 'Stop'
if (-not $Workspace) {
	$temp = Join-Path $env:LOCALAPPDATA 'Temp'
	if (-not (Test-Path -LiteralPath $temp -PathType Container)) { throw "Local temporary directory is missing: $temp" }
	$root = Join-Path $temp 'opencode'
	New-Item -ItemType Directory -Path $root -Force | Out-Null
	$Workspace = Join-Path $root ('web-test-' + [Guid]::NewGuid().ToString('N'))
}
$Workspace = [IO.Path]::GetFullPath($Workspace)
if (Test-Path -LiteralPath $Workspace) {
	throw "Workspace already exists: $Workspace. Choose a new directory; bootstrap does not overwrite prior runs."
}
$parent = Split-Path -Parent $Workspace
if (-not (Test-Path -LiteralPath $parent -PathType Container)) {
	throw "Create the parent directory first: $parent"
}
$template = Join-Path (Split-Path -Parent $PSScriptRoot) 'templates\Driver'
if (-not (Test-Path -LiteralPath (Join-Path $template 'Driver.csproj'))) {
	throw 'The skill is incomplete: templates/Driver/Driver.csproj is missing.'
}
Get-Command dotnet -ErrorAction Stop | Out-Null
New-Item -ItemType Directory -Path $Workspace | Out-Null
Copy-Item -LiteralPath $template -Destination (Join-Path $Workspace 'Driver') -Recurse
foreach ($name in @('run', 'screenshots', 'browsers')) {
	New-Item -ItemType Directory -Path (Join-Path $Workspace $name) | Out-Null
}
Set-Content -LiteralPath (Join-Path $Workspace '.ewl-web-test') -Value 'Disposable EWL web-test workspace' -Encoding UTF8
$previousBrowserPath = $env:PLAYWRIGHT_BROWSERS_PATH
try {
	$env:PLAYWRIGHT_BROWSERS_PATH = Join-Path $Workspace 'browsers'
	& dotnet build (Join-Path $Workspace 'Driver\Driver.csproj') --nologo
	if ($LASTEXITCODE -ne 0) { throw 'Driver build failed. A .NET 10 SDK and NuGet access are required.' }
	& (Join-Path $Workspace 'Driver\bin\Debug\net10.0\playwright.ps1') install chromium
	if ($LASTEXITCODE -ne 0) { throw 'Chromium installation failed. Check download access.' }
}
finally {
	$env:PLAYWRIGHT_BROWSERS_PATH = $previousBrowserPath
}
Write-Output "Workspace ready: $Workspace"
Write-Output 'Run scripts/Run.ps1 with this workspace and -Scenario self-check to verify the browser.'
