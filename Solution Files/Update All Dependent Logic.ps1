cd ..
if( Test-Path packages\EWL ) { Remove-Item packages\EWL -Recurse }

$packageIdFilePath = "Library\Configuration\EWL Package Id.txt"
$packageId = if( Test-Path $packageIdFilePath ) { Get-Content $packageIdFilePath } else { "PackageId:Ewl" }
$packageVersion = ( & "Solution Files\nuget" list $packageId -Prerelease -NonInteractive ).Split( " " )[1]
New-Item packages\EWL\packages.config -Force -ItemType file -Value "<?xml version=`"1.0`" encoding=`"utf-8`"?><packages><package id=`"$packageId`" version=`"$packageVersion`" /></packages>" | Out-Null
& "Solution Files\nuget" restore packages\EWL\packages.config -PackagesDirectory packages\EWL -NonInteractive

cd "packages\EWL\Ewl*\Development Utility"
& .\EnterpriseWebLibrary.DevelopmentUtility ..\..\..\.. UpdateAllDependentLogic