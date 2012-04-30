using RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel.Logic;

namespace RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel {
	internal interface RecognizedInstallation: ExistingInstallation, KnownInstallation {
		RecognizedInstallationLogic RecognizedInstallationLogic { get; }
	}
}