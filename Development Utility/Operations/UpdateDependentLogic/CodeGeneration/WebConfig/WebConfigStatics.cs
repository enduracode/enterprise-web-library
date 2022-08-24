using System.Text;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.InstallationSupportUtility;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebConfig; 

internal class WebConfigStatics {
	internal static void GenerateWebConfig( WebApplication application ) {
		try {
			File.WriteAllText(
				application.WebConfigFilePath,
				File.ReadAllText( EwlStatics.CombinePaths( ConfigurationStatics.FilesFolderPath, "web.config" ) ),
				Encoding.UTF8 );
		}
		catch( Exception e ) {
			const string message = "Failed to write web configuration file.";
			if( e is UnauthorizedAccessException )
				throw new UserCorrectableException( message, e );
			throw new ApplicationException( message, e );
		}
	}
}