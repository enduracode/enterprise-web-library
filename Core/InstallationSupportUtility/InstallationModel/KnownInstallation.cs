namespace EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel {
	public interface KnownInstallation: Installation {
		int Id { get; }
		KnownSystemLogic KnownSystemLogic { get; }
	}
}