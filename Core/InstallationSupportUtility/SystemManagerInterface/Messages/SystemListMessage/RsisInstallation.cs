using EnterpriseWebLibrary.Configuration;

namespace EnterpriseWebLibrary.InstallationSupportUtility.SystemManagerInterface.Messages.SystemListMessage;

public partial class RsisInstallation {
	public string FullName =>
		InstallationConfiguration.GetFullNameFromSystemAndInstallationNames(
			SystemManagerConnectionStatics.SystemList.GetSystemByInstallationId( Id )!.Name,
			Name );

	public string FullShortName =>
		InstallationConfiguration.GetFullShortNameFromSystemAndInstallationNames(
			SystemManagerConnectionStatics.SystemList.GetSystemByInstallationId( Id )!.ShortName,
			ShortName );

	public override string ToString() => FullName;
}