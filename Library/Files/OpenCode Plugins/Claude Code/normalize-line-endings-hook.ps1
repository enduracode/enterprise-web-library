$input = [Console]::In.ReadToEnd() | ConvertFrom-Json

$filePath = $input.tool_input.file_path
if( -not $filePath ) { exit 0 }
if( -not (Test-Path $filePath) ) { exit 0 }

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
& "$scriptDir/normalize-line-endings.ps1" -FilePath $filePath
