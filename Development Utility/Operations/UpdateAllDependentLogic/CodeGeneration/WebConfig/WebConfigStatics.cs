using System;
using System.IO;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.Configuration.SystemDevelopment;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;
using RedStapler.StandardLibrary.InstallationSupportUtility;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebConfig {
	internal class WebConfigStatics {
		internal static void GenerateWebConfig( WebProject webProject, string webProjectPath, string systemName ) {
			var templateContents = File.ReadAllText( StandardLibraryMethods.CombinePaths( AppTools.FilesFolderPath, "Template.config" ) );

			var useCertificateAuth = webProject.useCertificateAuthenticationSpecified && webProject.useCertificateAuthentication;

			templateContents = templateContents.Replace( "@@AuthenticationMode", useCertificateAuth ? "None" : "Forms" );
			templateContents = templateContents.Replace( "@@FormsAuthenticationName",
			                                             ( webProject.name + systemName + "FormsAuth" ).RemoveCommonNonAlphaNumericCharacters().Replace( " ", "" ) );

			templateContents = templateContents.Replace( "@@SessionTimeout", ( (int)UserManagementStatics.SessionDuration.TotalMinutes ).ToString() );

			const string cacheTimeoutTimeSpan = "10:00:00"; // 10 hours
			templateContents = templateContents.Replace( "@@CacheTimeout", cacheTimeoutTimeSpan );

			templateContents = templateContents.Replace( "@@CertificateAuthenticationModulePlace",
			                                             useCertificateAuth
			                                             	? "<add name=\"CertificateAuthentication\" type=\"RedStapler.StandardLibrary.CertificateAuthenticationModule, RedStapler.StandardLibrary\"/>"
			                                             	: "" );

			try {
				File.WriteAllText( StandardLibraryMethods.CombinePaths( webProjectPath, "Web.config" ), templateContents );
				File.WriteAllText( StandardLibraryMethods.CombinePaths( webProjectPath, "Installed.config" ),
				                   templateContents.Replace( "debug=\"true\"", "debug=\"false\"" ) );
			}
			catch( Exception e ) {
				const string message = "Failed to write web configuration files.";
				if( e is UnauthorizedAccessException )
					throw new UserCorrectableException( message, e );
				throw new ApplicationException( message, e );
			}
		}
	}
}