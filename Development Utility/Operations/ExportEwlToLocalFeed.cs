using System.Collections.Generic;
using System.IO;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.InstallationSupportUtility;
using EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations {
	internal class ExportEwlToLocalFeed: Operation {
		private static readonly Operation instance = new ExportEwlToLocalFeed();
		public static Operation Instance => instance;
		private ExportEwlToLocalFeed() {}

		bool Operation.IsValid( Installation genericInstallation ) =>
			genericInstallation is RecognizedDevelopmentInstallation installation && installation.DevelopmentInstallationLogic.SystemIsEwl;

		void Operation.Execute( Installation genericInstallation, IReadOnlyList<string> arguments, OperationResult operationResult ) {
			var installation = genericInstallation as RecognizedDevelopmentInstallation;
			var localNuGetFeedFolderPath = EwlStatics.CombinePaths( ConfigurationStatics.RedStaplerFolderPath, "Local NuGet Feed" );

			// nuget.exe has problems if the folder doesn't exist.
			Directory.CreateDirectory( localNuGetFeedFolderPath );

			ExportLogic.CreateEwlNuGetPackage( installation, ExportLogic.GetPackagingConfiguration( installation ), true, localNuGetFeedFolderPath, null );
		}
	}
}