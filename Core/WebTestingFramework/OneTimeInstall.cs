﻿using System.ServiceProcess;
using EnterpriseWebLibrary.Configuration;
using Tewl.IO;

namespace EnterpriseWebLibrary.WebTestingFramework;

internal class OneTimeInstall {
	// Bill NOTE: This needs to be reimplemented to simply set up Chrome for Testing and ChromeDriver, if they’re not already present.
	/// <summary>
	/// This is the code to install and start the SeleniumRC service.
	/// </summary>
	public static void InstallSeleniumServiceIfNecessary() {
		var supportFilesDestinationPath = EwlStatics.CombinePaths( ConfigurationStatics.EwlFolderPath, "Selenium Support" );
		const string serviceName = "SeleniumRC";
		const string seleniumJarFile = "selenium-server.jar";
		const string srvany = "srvany.exe";

		var seleniumServerService = ServiceController.GetServices().Where( s => s.DisplayName == serviceName ).SingleOrDefault();
		if( !Directory.Exists( supportFilesDestinationPath ) || seleniumServerService == null ) {
			// Wipe out any possible pre-existing configuration.
			IoMethods.DeleteFolder( supportFilesDestinationPath );
			if( seleniumServerService != null ) {
				// Delete the service and remove the registry values.
				TewlContrib.ProcessTools.RunProgram( "sc", "delete " + serviceName, "", true );
				seleniumServerService = null;
			}


			// NOTE: This will work because the only machines running tests are dev machines and integration machines, and both of those not only should have this in their Vault
			// tree, but they are guaranteed to have gotten latest on the Standard Library system before attempting to run a test (since you can't run a test until you've built).
			// Still, there may be a more robust way to do this in the future.
			// If we do keep this solution, you have to ask yourself why it makes sense to store the files here just to copy them to Red Stapler/Selenium Support. The only good reason
			// may be that the Vault tree could be prone to full deletion/re-getting, and that would fail if a service was referencing a file inside the tree.

			// NOTE: This path is probably wrong, and should not be hard-coded.
			const string supportFilesSourcePath = @"C:\Red Stapler Vault\Supporting Files\Standard Library\Solution Files\Selenium Support";

			var srvanyDestinationPath = EwlStatics.CombinePaths( supportFilesDestinationPath, srvany );
			// Create c:\Red Stapler\Selenium Support
			Directory.CreateDirectory( supportFilesDestinationPath );
			IoMethods.CopyFile( EwlStatics.CombinePaths( supportFilesSourcePath, srvany ), srvanyDestinationPath );
			IoMethods.CopyFile(
				EwlStatics.CombinePaths( supportFilesSourcePath, seleniumJarFile ),
				EwlStatics.CombinePaths( supportFilesDestinationPath, seleniumJarFile ) );


			const string serviceRegCmd = @"ADD HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\" + serviceName + "\\Parameters  /v ";
			const string regDataType = " /t REG_SZ /d ";
			const string javaFolder = @"C:\Program Files\Java\jre6\bin";
			var parametersToSeleniumServer = "";
			if( ConfigurationStatics.IsDevelopmentInstallation ) {
				var firefoxProfileFolderPath = EwlStatics.CombinePaths( supportFilesDestinationPath, "Firefox" );
				Directory.CreateDirectory( firefoxProfileFolderPath );
				parametersToSeleniumServer = " -firefoxProfileTemplate \\\"" + firefoxProfileFolderPath + "\\\"";
			}

			// This is the code to add the registry parameters to the Selenium Server.  This only needs to be run once.
			TewlContrib.ProcessTools.RunProgram( "sc", "create " + serviceName + " binPath= \"" + srvanyDestinationPath + "\" start= auto", "", true );
			TewlContrib.ProcessTools.RunProgram(
				"REG",
				serviceRegCmd + "Application" + regDataType + "\"" + EwlStatics.CombinePaths( javaFolder, "java.exe" ) + "\"",
				"",
				true );
			TewlContrib.ProcessTools.RunProgram( "REG", serviceRegCmd + "AppDirectory" + regDataType + "\"" + supportFilesDestinationPath + "\" ", "", true );
			TewlContrib.ProcessTools.RunProgram(
				"REG",
				serviceRegCmd + "AppParameters" + regDataType + "\"-Xrs -jar " + seleniumJarFile + parametersToSeleniumServer + "\"",
				"",
				true );

			// Wait for the service to be created
			while( seleniumServerService == null )
				seleniumServerService = ServiceController.GetServices().Where( s => s.DisplayName == serviceName ).SingleOrDefault();
		}

		if( seleniumServerService.Status != ServiceControllerStatus.Running ) {
			seleniumServerService.Start();
			seleniumServerService.WaitForStatus( ServiceControllerStatus.Running );
		}
	}
}