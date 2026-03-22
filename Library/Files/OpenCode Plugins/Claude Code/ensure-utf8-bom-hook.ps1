$input = [Console]::In.ReadToEnd() | ConvertFrom-Json

$filePath = $input.tool_input.file_path
if( -not $filePath ) { exit 0 }

$extensions = @(".cs", ".csproj", ".cshtml")
$ext = [System.IO.Path]::GetExtension($filePath)
if( $extensions -notcontains $ext ) { exit 0 }
if( -not (Test-Path $filePath) ) { exit 0 }

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
& "$scriptDir/ensure-utf8-bom.ps1" -FilePath $filePath
