using System.IO;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.Configuration;
using RedStapler.StandardLibrary.InstallationSupportUtility;
using RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations {
	internal class ExportEwlToLocalFeed: Operation {
		private static readonly Operation instance = new ExportEwlToLocalFeed();
		public static Operation Instance { get { return instance; } }
		private ExportEwlToLocalFeed() {}

		bool Operation.IsValid( Installation genericInstallation ) {
			var installation = genericInstallation as RecognizedDevelopmentInstallation;
			return installation != null && installation.DevelopmentInstallationLogic.SystemIsEwl;
		}

		void Operation.Execute( Installation genericInstallation, OperationResult operationResult ) {
			var installation = genericInstallation as RecognizedDevelopmentInstallation;
			var localNuGetFeedFolderPath = EwlStatics.CombinePaths( ConfigurationStatics.RedStaplerFolderPath, "Local NuGet Feed" );

			// NuGet.exe has problems if the folder doesn't exist.
			Directory.CreateDirectory( localNuGetFeedFolderPath );

			ExportLogic.CreateEwlNuGetPackage( installation, true, localNuGetFeedFolderPath, null );
		}
	}
}