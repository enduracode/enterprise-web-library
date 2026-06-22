param( $installPath, $toolsPath, $package )

New-Module -Name 'EWL Development Utility' -ScriptBlock {
$installPath = $args[0]
$installationPath = Split-Path -Path $dte.Solution.FileName -Parent

function Update-EwlPackages {
	[CmdletBinding()]
	Param()
	Process {
		Write-Host
		Write-Host 'Please copy and run this command, removing the -pre switch if you want the latest stable version:'
		Write-Host
		Write-Host 'foreach( $p in Get-Package | where Id -Like ''Ewl*'' | select -ExpandProperty Id | sort -Unique ) { Update-Package $p -pre }' -ForegroundColor DarkGreen
		Write-Host
	}
}

function Initialize-InstallationConfiguration {
	[CmdletBinding()]
	Param()
	Process {
		& "$installPath\tools\Development Utility\EnterpriseWebLibrary.DevelopmentUtility" $installationPath CreateInstallationConfiguration
	}
}

function Update-Data {
	[CmdletBinding()]
	Param(
		[ValidateLength(1,100)]
		$Source = 'Default',
		[Switch]$ForceNewPackageDownload
	)
	Process {
		& "$installPath\tools\Development Utility\EnterpriseWebLibrary.DevelopmentUtility" $installationPath UpdateData $Source $ForceNewPackageDownload
	}
}

function Update-DependentLogic {
	[CmdletBinding()]
	Param()
	Process {
		& "$installPath\tools\Development Utility\EnterpriseWebLibrary.DevelopmentUtility" $installationPath UpdateDependentLogic
	}
}

function ExportLogic {
	[CmdletBinding()]
	Param()
	Process {
		& "$installPath\tools\Development Utility\EnterpriseWebLibrary.DevelopmentUtility" $installationPath ExportLogic
	}
}

function Measure-LogicSize {
	[CmdletBinding()]
	Param()
	Process {
		& "$installPath\tools\Development Utility\EnterpriseWebLibrary.DevelopmentUtility" $installationPath GetLogicSize
	}
}

function InstallAndStartServices {
	[CmdletBinding()]
	Param()
	Process {
		& "$installPath\tools\Development Utility\EnterpriseWebLibrary.DevelopmentUtility" $installationPath InstallAndStartServices
	}
}

function StopAndUninstallServices {
	[CmdletBinding()]
	Param()
	Process {
		& "$installPath\tools\Development Utility\EnterpriseWebLibrary.DevelopmentUtility" $installationPath StopAndUninstallServices
	}
}

} -ArgumentList $installPath