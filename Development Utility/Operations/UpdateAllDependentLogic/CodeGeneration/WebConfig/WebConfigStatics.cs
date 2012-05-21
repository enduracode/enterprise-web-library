using System;
using System.IO;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.Configuration.SystemDevelopment;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;
using RedStapler.StandardLibrary.InstallationSupportUtility;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebConfig {
	internal class WebConfigStatics {
		internal static void GenerateWebConfig( WebProject webProject, string webProjectPath, string systemName ) {
			if( !File.Exists( StandardLibraryMethods.CombinePaths( webProjectPath, "Web.config" ) ) )
				throw new UserCorrectableException( "The Web.config file is missing." );
			var webConfigContents = File.ReadAllText( StandardLibraryMethods.CombinePaths( webProjectPath, "Web.config" ) );

			webConfigContents = removeElement( webConfigContents, "appSettings" );
			webConfigContents = removeElement( webConfigContents, "system.web" );
			webConfigContents = removeElement( webConfigContents, "system.webServer" );

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

			webConfigContents = webConfigContents.Replace( "<configuration>", templateContents );

			try {
				File.WriteAllText( StandardLibraryMethods.CombinePaths( webProjectPath, "Web.config" ), webConfigContents );
			}
			catch( Exception e ) {
				const string message = "Failed to write web configuration file.";
				if( e is UnauthorizedAccessException )
					throw new UserCorrectableException( message, e );
				throw new ApplicationException( message, e );
			}
		}

		private static string removeElement( string webConfigContents, string element ) {
			var index = webConfigContents.IndexOf( "<" + element + ">" );
			if( index == -1 )
				return webConfigContents;
			var endTag = "</" + element + ">";
			var endTagIndex = webConfigContents.IndexOf( endTag );
			if( endTagIndex == -1 )
				throw new UserCorrectableException( "End tag not found." );
			return webConfigContents.Remove( index, endTagIndex + endTag.Length - index );
		}
	}
}