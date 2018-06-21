param( $installPath, $toolsPath, $package )

New-Module -Name 'EWL Development Utility' -ScriptBlock {
$installPath = $args[0]

function Update-Data {
	[CmdletBinding()]
	Param( $Source )
	Process {
		& "$installPath\Development Utility\EnterpriseWebLibrary.DevelopmentUtility" $installPath\..\.. UpdateData $Source
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