param( $installPath, $toolsPath, $package )

New-Module -ScriptBlock {
$installPath = $args[0]

function UpdateDependentLogic {
	[CmdletBinding()]
	Param()
	Process {
		& "$installPath\Development Utility\EnterpriseWebLibrary.DevelopmentUtility" $installPath\..\.. UpdateAllDependentLogic
		Write-Host "Adding binding redirects:"
		Add-BindingRedirect
	}
}

function ExportLogic {
	[CmdletBinding()]
	Param()
	Process {
		& "$installPath\Development Utility\EnterpriseWebLibrary.DevelopmentUtility" $installPath\..\.. ExportLogic
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