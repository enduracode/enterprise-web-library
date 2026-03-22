$hookData = [Console]::In.ReadToEnd() | ConvertFrom-Json

$filePath = $hookData.tool_input.file_path
if( -not $filePath ) { exit 0 }
if( -not (Test-Path $filePath) ) { exit 0 }

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

$bomExtensions = @(".cs", ".csproj", ".cshtml")
$ext = [System.IO.Path]::GetExtension($filePath)
if( $bomExtensions -contains $ext ) {
	& "$scriptDir/ensure-utf8-bom.ps1" -FilePath $filePath
}

& "$scriptDir/normalize-line-endings.ps1" -FilePath $filePath
