cd ..
if( Test-Path packages\EWL ) { Remove-Item packages\EWL -Recurse }

$packagingConfigurationFilePath = 'Library\Configuration\Installation\Installations\Packaging.xml'
$packageId = if( Test-Path $packagingConfigurationFilePath ) { ( [xml]( Get-Content $packagingConfigurationFilePath ) ).PackagingConfiguration.SystemShortName } else { 'Ewl' }
$searchTerm = if( $packageId -eq "Ewl" ) { "PackageId:$packageId" } else { $packageId }
$packageVersion = ( ( & "Solution Files\nuget" list $searchTerm -Prerelease -NonInteractive ) | Select-String $packageId -SimpleMatch | Select-Object -First 1 ).Line.Split( " " )[1]
New-Item packages\EWL\packages.config -Force -ItemType file -Value "<?xml version=`"1.0`" encoding=`"utf-8`"?><packages><package id=`"$packageId`" version=`"$packageVersion`" /></packages>" | Out-Null
& "Solution Files\nuget" restore packages\EWL\packages.config -PackagesDirectory packages\EWL -NonInteractive

cd "packages\EWL\Ewl*\tools\Development Utility"
& .\EnterpriseWebLibrary.DevelopmentUtility ..\..\..\..\.. UpdateDependentLogic