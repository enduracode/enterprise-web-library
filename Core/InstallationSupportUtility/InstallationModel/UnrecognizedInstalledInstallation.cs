using JetBrains.Annotations;

namespace EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;

[ PublicAPI ]
public class UnrecognizedInstalledInstallation: ExistingInstalledInstallation, UnrecognizedInstallation {
	private readonly GeneralInstallationLogic generalInstallationLogic;
	private readonly ExistingInstallationLogic existingInstallationLogic;
	private readonly ExistingInstalledInstallationLogic existingInstalledInstallationLogic;
	private readonly UnrecognizedInstallationLogic unrecognizedInstallationLogic;

	public UnrecognizedInstalledInstallation(
		GeneralInstallationLogic generalInstallationLogic, ExistingInstallationLogic existingInstallationLogic,
		ExistingInstalledInstallationLogic existingInstalledInstallationLogic, UnrecognizedInstallationLogic unrecognizedInstallationLogic ) {
		this.generalInstallationLogic = generalInstallationLogic;
		this.existingInstallationLogic = existingInstallationLogic;
		this.existingInstalledInstallationLogic = existingInstalledInstallationLogic;
		this.unrecognizedInstallationLogic = unrecognizedInstallationLogic;
	}

	public override string ToString() => LatestFullName;

	public string LatestFullName => existingInstallationLogic.RuntimeConfiguration.FullName;

	public string LatestFullShortName => existingInstallationLogic.RuntimeConfiguration.FullShortName;

	public GeneralInstallationLogic GeneralLogic => generalInstallationLogic;

	public ExistingInstallationLogic ExistingInstallationLogic => existingInstallationLogic;

	public ExistingInstalledInstallationLogic ExistingInstalledInstallationLogic => existingInstalledInstallationLogic;

	public UnrecognizedInstallationLogic UnrecognizedInstallationLogic => unrecognizedInstallationLogic;
}