param( $installPath, $toolsPath, $package )

New-Module -Name 'EWL Development Utility' -ScriptBlock {
$installPath = $args[0]
$installationPath = Split-Path -Path $dte.Solution.FileName -Parent

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