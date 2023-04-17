cd ..
if( Test-Path "Latest Package" ) { Remove-Item "Latest Package" -Recurse }

$packagingConfigurationFilePath = 'Library\Configuration\Installation\Installations\Packaging.xml'
$packageId = if( Test-Path $packagingConfigurationFilePath ) { ( [xml]( Get-Content $packagingConfigurationFilePath ) ).PackagingConfiguration.SystemShortName } else { 'Ewl' }
& "Solution Files\nuget" install $packageId -DependencyVersion Ignore -ExcludeVersion -NonInteractive -OutputDirectory "Latest Package" -PackageSaveMode nuspec -PreRelease

cd "Latest Package\$packageId\tools\Development Utility"
& .\EnterpriseWebLibrary.DevelopmentUtility ..\..\..\.. UpdateDependentLogic