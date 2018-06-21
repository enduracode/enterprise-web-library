param( $installPath, $toolsPath, $package )

New-Module -Name 'EWL Development Utility' -ScriptBlock {
$installPath = $args[0]

function Update-Data {
	[CmdletBinding()]
	Param(
		[ValidateLength(1,100)]
		$Source = 'Default',
		[Switch]$ForceNewPackageDownload
	)
	Process {
		& "$installPath\Development Utility\EnterpriseWebLibrary.DevelopmentUtility" $installPath\..\.. UpdateData $Source $ForceNewPackageDownload
	}
}

function Update-DependentLogic {
	[CmdletBinding()]
	Param()
	Process {
		& "$installPath\Development Utility\EnterpriseWebLibrary.DevelopmentUtility" $installPath\..\.. UpdateDependentLogic
	}
}

function ExportLogic {
	[CmdletBinding()]
	Param()
	Process {
		& "$installPath\Development Utility\EnterpriseWebLibrary.DevelopmentUtility" $installPath\..\.. ExportLogic
	}
}

function Measure-LogicSize {
	[CmdletBinding()]
	Param()
	Process {
		& "$installPath\Development Utility\EnterpriseWebLibrary.DevelopmentUtility" $installPath\..\.. GetLogicSize
	}
}

function InstallAndStartServices {
	[CmdletBinding()]
	Param()
	Process {
		& "$installPath\Development Utility\EnterpriseWebLibrary.DevelopmentUtility" $installPath\..\.. InstallAndStartServices
	}
}

function StopAndUninstallServices {
	[CmdletBinding()]
	Param()
	Process {
		& "$installPath\Development Utility\EnterpriseWebLibrary.DevelopmentUtility" $installPath\..\.. StopAndUninstallServices
	}
}

} -ArgumentList $installPath