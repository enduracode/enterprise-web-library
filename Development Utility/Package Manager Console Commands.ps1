param( $installPath, $toolsPath, $package )

New-Module -ScriptBlock {

$installPath = $args[0]

function UpdateDependentLogic {
	[CmdletBinding()]
	Param()
	Process {
		& "$installPath\Development Utility\EnterpriseWebLibrary.DevelopmentUtility" $installPath\..\.. UpdateAllDependentLogic
	}
}

} -ArgumentList $installPath