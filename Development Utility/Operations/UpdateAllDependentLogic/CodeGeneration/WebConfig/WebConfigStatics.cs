using System;
using System.IO;
using System.Xml;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.Configuration;
using RedStapler.StandardLibrary.Configuration.SystemDevelopment;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;
using RedStapler.StandardLibrary.InstallationSupportUtility;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebConfig {
	internal class WebConfigStatics {
		internal static void GenerateWebConfig( WebProject webProject, string webProjectPath ) {
			var sections = new XmlDocument { PreserveWhitespace = true };
			using( var reader = new StringReader( getSectionString( webProject ) ) )
				sections.Load( reader );

			if( !File.Exists( EwlStatics.CombinePaths( webProjectPath, "Web.config" ) ) )
				throw new UserCorrectableException( "The Web.config file is missing." );
			var webConfig = new XmlDocument { PreserveWhitespace = true };
			webConfig.Load( EwlStatics.CombinePaths( webProjectPath, "Web.config" ) );

			replaceSection( sections, webConfig, "appSettings" );
			replaceSection( sections, webConfig, "system.web" );
			replaceSection( sections, webConfig, "system.webServer" );

			try {
				webConfig.Save( EwlStatics.CombinePaths( webProjectPath, "Web.config" ) );
			}
			catch( Exception e ) {
				const string message = "Failed to write web configuration file.";
				if( e is UnauthorizedAccessException )
					throw new UserCorrectableException( message, e );
				throw new ApplicationException( message, e );
			}
		}

		private static string getSectionString( WebProject webProject ) {
			var sections = File.ReadAllText( EwlStatics.CombinePaths( ConfigurationStatics.FilesFolderPath, "Template.config" ) );

			sections = sections.Replace( "@@SessionTimeout", ( (int)FormsAuthStatics.SessionDuration.TotalMinutes ).ToString() );

			var useCertificateAuth = webProject.useCertificateAuthenticationSpecified && webProject.useCertificateAuthentication;
			sections = sections.Replace(
				"@@CertificateAuthenticationModulePlace",
				useCertificateAuth
					? "<add name=\"CertificateAuthentication\" type=\"RedStapler.StandardLibrary.CertificateAuthenticationModule, EnterpriseWebLibrary\"/>"
					: "" );

			const string cacheTimeoutTimeSpan = "10:00:00"; // 10 hours
			sections = sections.Replace( "@@CacheTimeout", cacheTimeoutTimeSpan );

			return sections;
		}

		private static void replaceSection( XmlDocument source, XmlDocument destination, string section ) {
			const string configurationNodeName = "configuration";
			var newChild = destination.ImportNode( source[ configurationNodeName ][ section ], true );
			var destinationConfigurationNode = destination[ configurationNodeName ];

			var oldChild = destinationConfigurationNode[ section ];
			if( oldChild != null )
				destinationConfigurationNode.ReplaceChild( newChild, oldChild );
			else
				destinationConfigurationNode.AppendChild( newChild );
		}
	}
}