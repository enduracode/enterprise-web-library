namespace EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;

public class UnrecognizedDevelopmentInstallation: UnrecognizedInstallation, DevelopmentInstallation {
	private readonly GeneralInstallationLogic generalInstallationLogic;
	private readonly ExistingInstallationLogic existingInstallationLogic;
	private readonly UnrecognizedInstallationLogic unrecognizedInstallationLogic;
	private readonly DevelopmentInstallationLogic developmentInstallationLogic;

	public UnrecognizedDevelopmentInstallation(
		GeneralInstallationLogic generalInstallationLogic, ExistingInstallationLogic existingInstallationLogic,
		UnrecognizedInstallationLogic unrecognizedInstallationLogic ) {
		this.generalInstallationLogic = generalInstallationLogic;
		this.existingInstallationLogic = existingInstallationLogic;
		this.unrecognizedInstallationLogic = unrecognizedInstallationLogic;
		developmentInstallationLogic = new DevelopmentInstallationLogic( generalInstallationLogic, existingInstallationLogic, null );
	}

	public override string ToString() => LatestFullName;

	public string LatestFullName => existingInstallationLogic.RuntimeConfiguration.FullName;

	public string LatestFullShortName => existingInstallationLogic.RuntimeConfiguration.FullShortName;

	public GeneralInstallationLogic GeneralLogic => generalInstallationLogic;

	public ExistingInstallationLogic ExistingInstallationLogic => existingInstallationLogic;

	public UnrecognizedInstallationLogic UnrecognizedInstallationLogic => unrecognizedInstallationLogic;

	public DevelopmentInstallationLogic DevelopmentInstallationLogic => developmentInstallationLogic;

	int DevelopmentInstallation.CurrentMajorVersion => 1;
	int DevelopmentInstallation.NextBuildNumber => 1;
}