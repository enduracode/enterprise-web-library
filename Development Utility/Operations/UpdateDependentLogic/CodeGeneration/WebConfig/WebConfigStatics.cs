﻿using System;
using System.IO;
using System.Xml;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.Configuration.SystemDevelopment;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using EnterpriseWebLibrary.InstallationSupportUtility;
using Humanizer;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebConfig {
	internal class WebConfigStatics {
		internal static void GenerateWebConfig( WebApplication application, WebProject project ) {
			var sections = new XmlDocument { PreserveWhitespace = true };
			using( var reader = new StringReader( getSectionString( project ) ) )
				sections.Load( reader );

			if( !File.Exists( application.WebConfigFilePath ) )
				throw new UserCorrectableException( "The {0} file is missing.".FormatWith( WebApplication.WebConfigFileName ) );
			var webConfig = new XmlDocument { PreserveWhitespace = true };
			webConfig.Load( application.WebConfigFilePath );

			replaceSection( sections, webConfig, "appSettings" );
			replaceSection( sections, webConfig, "system.web" );
			replaceSection( sections, webConfig, "system.webServer" );

			try {
				webConfig.Save( application.WebConfigFilePath );
			}
			catch( Exception e ) {
				const string message = "Failed to write web configuration file.";
				if( e is UnauthorizedAccessException )
					throw new UserCorrectableException( message, e );
				throw new ApplicationException( message, e );
			}
		}

		private static string getSectionString( WebProject project ) {
			var sections = File.ReadAllText( EwlStatics.CombinePaths( ConfigurationStatics.FilesFolderPath, "Template.config" ) );

			if( project.UsesEntityFrameworkSpecified && project.UsesEntityFramework )
				sections = sections.Replace(
					"<compilation debug=\"true\" targetFramework=\"4.7.2\" />",
					"<compilation debug=\"true\" targetFramework=\"4.7.2\"><buildProviders><remove extension=\".edmx\" /></buildProviders></compilation>" );

			sections = sections.Replace( "@@SessionTimeout", ( (int)AuthenticationStatics.SessionDuration.TotalMinutes ).ToString() );

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