namespace EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;

public class RecognizedInstalledInstallation: ExistingInstalledInstallation, KnownInstalledInstallation, RecognizedInstallation {
	private readonly GeneralInstallationLogic generalInstallationLogic;
	private readonly ExistingInstallationLogic existingInstallationLogic;
	private readonly ExistingInstalledInstallationLogic existingInstalledInstallationLogic;
	private readonly KnownSystemLogic knownSystemLogic;
	private readonly KnownInstallationLogic knownInstallationLogic;
	private readonly RecognizedInstallationLogic recognizedInstallationLogic;

	public RecognizedInstalledInstallation(
		GeneralInstallationLogic generalInstallationLogic, ExistingInstallationLogic existingInstallationLogic,
		ExistingInstalledInstallationLogic existingInstalledInstallationLogic, KnownSystemLogic knownSystemLogic,
		RecognizedInstallationLogic recognizedInstallationLogic ) {
		this.generalInstallationLogic = generalInstallationLogic;
		this.existingInstallationLogic = existingInstallationLogic;
		this.existingInstalledInstallationLogic = existingInstalledInstallationLogic;
		this.knownSystemLogic = knownSystemLogic;
		knownInstallationLogic = new KnownInstallationLogic(
			SystemManagerConnectionStatics.SystemList.GetInstallationById( existingInstallationLogic.RuntimeConfiguration.RsisInstallationId!.Value )! );
		this.recognizedInstallationLogic = recognizedInstallationLogic;
	}

	public override string ToString() => LatestFullName;

	public int Id => knownInstallationLogic.RsisInstallation.Id;

	public string LatestFullName => knownInstallationLogic.RsisInstallation.FullName;

	public string LatestFullShortName => knownInstallationLogic.RsisInstallation.FullShortName;

	public GeneralInstallationLogic GeneralLogic => generalInstallationLogic;

	public ExistingInstallationLogic ExistingInstallationLogic => existingInstallationLogic;

	public ExistingInstalledInstallationLogic ExistingInstalledInstallationLogic => existingInstalledInstallationLogic;

	public KnownSystemLogic KnownSystemLogic => knownSystemLogic;

	public KnownInstallationLogic KnownInstallationLogic => knownInstallationLogic;

	public RecognizedInstallationLogic RecognizedInstallationLogic => recognizedInstallationLogic;
}