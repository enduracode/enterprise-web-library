using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.InstallationSupportUtility;
using EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations;

internal class ExportEwlToLocalFeed: Operation {
	private static readonly Operation instance = new ExportEwlToLocalFeed();
	public static Operation Instance => instance;
	private ExportEwlToLocalFeed() {}

	bool Operation.IsValid( Installation genericInstallation ) =>
		genericInstallation is RecognizedDevelopmentInstallation installation &&
		( installation.DevelopmentInstallationLogic is { SystemIsEwl: true } || installation.SystemIsTewl() );

	void Operation.Execute( Installation genericInstallation, IReadOnlyList<string> arguments, OperationResult operationResult ) {
		var installation = (RecognizedDevelopmentInstallation)genericInstallation;
		var localNuGetFeedFolderPath = EwlStatics.CombinePaths( ConfigurationStatics.EwlFolderPath, "Local NuGet Feed" );

		// nuget.exe has problems if the folder doesn't exist.
		Directory.CreateDirectory( localNuGetFeedFolderPath );

		ExportLogic.CreateEwlNuGetPackages(
			installation,
			ExportLogic.GetPackagingConfiguration( installation ),
			true,
			localNuGetFeedFolderPath,
			new bool?[] { null } );
	}
}