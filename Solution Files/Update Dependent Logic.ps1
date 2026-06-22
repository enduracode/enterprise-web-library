$ErrorActionPreference = 'Stop'

Set-Location ( Join-Path $PSScriptRoot '..' )
if( Test-Path "Latest DU Package" ) { Remove-Item -LiteralPath "\\?\$( ( Resolve-Path 'Latest DU Package' ).Path )" -Recurse }

$packagingConfigurationFilePath = 'Library\Configuration\Installation\Installations\Packaging.xml'
$packageId = if( Test-Path $packagingConfigurationFilePath ) { ( [xml]( Get-Content $packagingConfigurationFilePath ) ).PackagingConfiguration.SystemShortName } else { 'Ewl' }
& dotnet tool install "$packageId.DevelopmentUtility" --tool-path "Latest DU Package" --no-cache --prerelease

& "Latest DU Package\ewl" UpdateDependentLogic