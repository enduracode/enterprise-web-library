using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Humanizer;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.Configuration;
using RedStapler.StandardLibrary.Configuration.InstallationStandard;
using RedStapler.StandardLibrary.Configuration.SystemDevelopment;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.InstallationSupportUtility;
using RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel;
using RedStapler.StandardLibrary.InstallationSupportUtility.RsisInterface.Messages;
using RedStapler.StandardLibrary.IO;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations {
	internal class ExportLogic: Operation {
		private static readonly Operation instance = new ExportLogic();

		internal static byte[] CreateEwlNuGetPackage( DevelopmentInstallation installation, bool useDebugAssembly, string outputFolderPath, bool? prerelease ) {
			var localExportDateAndTime = prerelease.HasValue ? null as DateTime? : DateTime.Now;

			IoMethods.ExecuteWithTempFolder(
				folderPath => {
					var ewlOutputFolderPath = EwlStatics.CombinePaths(
						installation.GeneralLogic.Path,
						AppStatics.CoreProjectName,
						EwlStatics.GetProjectOutputFolderPath( useDebugAssembly ) );
					var libFolderPath = EwlStatics.CombinePaths( folderPath, @"lib\net451-full" );
					foreach( var fileName in new[] { "dll", "pdb", "xml" }.Select( i => "EnterpriseWebLibrary." + i ) )
						IoMethods.CopyFile( EwlStatics.CombinePaths( ewlOutputFolderPath, fileName ), EwlStatics.CombinePaths( libFolderPath, fileName ) );

					IoMethods.CopyFile(
						EwlStatics.CombinePaths( installation.GeneralLogic.Path, @"Development Utility\Package Manager Console Commands.ps1" ),
						EwlStatics.CombinePaths( folderPath, @"tools\init.ps1" ) );

					var webSitePath = EwlStatics.CombinePaths( installation.GeneralLogic.Path, "Web Site" );
					var webProjectFilesFolderPath = EwlStatics.CombinePaths( folderPath, AppStatics.WebProjectFilesFolderName );
					IoMethods.CopyFolder(
						EwlStatics.CombinePaths( webSitePath, StaticFileHandler.EwfFolderName ),
						EwlStatics.CombinePaths( webProjectFilesFolderPath, StaticFileHandler.EwfFolderName ),
						false );
					IoMethods.CopyFile(
						EwlStatics.CombinePaths( webSitePath, AppStatics.StandardLibraryFilesFileName ),
						EwlStatics.CombinePaths( webProjectFilesFolderPath, AppStatics.StandardLibraryFilesFileName ) );

					const string duProjectAndFolderName = "Development Utility";
					IoMethods.CopyFolder(
						EwlStatics.CombinePaths(
							installation.GeneralLogic.Path,
							duProjectAndFolderName,
							EwlStatics.GetProjectOutputFolderPath( useDebugAssembly ) ),
						EwlStatics.CombinePaths( folderPath, duProjectAndFolderName ),
						false );
					packageGeneralFiles( installation, folderPath, false );
					IoMethods.CopyFolder(
						EwlStatics.CombinePaths(
							installation.ExistingInstallationLogic.RuntimeConfiguration.ConfigurationFolderPath,
							InstallationConfiguration.InstallationConfigurationFolderName,
							InstallationConfiguration.InstallationsFolderName,
							( !prerelease.HasValue || prerelease.Value ? "Testing" : "Live" ) ),
						EwlStatics.CombinePaths(
							folderPath,
							InstallationConfiguration.ConfigurationFolderName,
							InstallationConfiguration.InstallationConfigurationFolderName ),
						false );

					var manifestPath = EwlStatics.CombinePaths( folderPath, "Package.nuspec" );
					using( var writer = IoMethods.GetTextWriterForWrite( manifestPath ) )
						writeNuGetPackageManifest( installation, prerelease, localExportDateAndTime, writer );

					StatusStatics.SetStatus(
						EwlStatics.RunProgram(
							EwlStatics.CombinePaths( installation.GeneralLogic.Path, @"Solution Files\nuget" ),
							"pack \"" + manifestPath + "\" -OutputDirectory \"" + outputFolderPath + "\"",
							"",
							true ) );
				} );

			return
				File.ReadAllBytes(
					EwlStatics.CombinePaths(
						outputFolderPath,
						EwlNuGetPackageSpecificationStatics.GetNuGetPackageFileName(
							installation.ExistingInstallationLogic.RuntimeConfiguration.SystemShortName,
							installation.CurrentMajorVersion,
							!prerelease.HasValue || prerelease.Value ? installation.NextBuildNumber as int? : null,
							localExportDateAndTime: localExportDateAndTime ) ) );
		}

		private static void packageGeneralFiles( DevelopmentInstallation installation, string folderPath, bool includeDatabaseUpdates ) {
			// configuration files
			var configurationFolderPath = EwlStatics.CombinePaths( folderPath, InstallationConfiguration.ConfigurationFolderName );
			IoMethods.CopyFolder( installation.ExistingInstallationLogic.RuntimeConfiguration.ConfigurationFolderPath, configurationFolderPath, false );
			IoMethods.RecursivelyRemoveReadOnlyAttributeFromItem( configurationFolderPath );
			IoMethods.DeleteFolder( EwlStatics.CombinePaths( configurationFolderPath, InstallationConfiguration.InstallationConfigurationFolderName ) );
			IoMethods.DeleteFolder( EwlStatics.CombinePaths( configurationFolderPath, ConfigurationStatics.ProvidersFolderAndNamespaceName ) );
			if( !includeDatabaseUpdates )
				IoMethods.DeleteFile( EwlStatics.CombinePaths( configurationFolderPath, ExistingInstallationLogic.SystemDatabaseUpdatesFileName ) );
			IoMethods.DeleteFile( EwlStatics.CombinePaths( configurationFolderPath, InstallationConfiguration.SystemDevelopmentConfigurationFileName ) );
			IoMethods.DeleteFolder( EwlStatics.CombinePaths( configurationFolderPath, ".hg" ) ); // EWL uses a nested repository for configuration.
			IoMethods.DeleteFile( EwlStatics.CombinePaths( configurationFolderPath, "Update All Dependent Logic.bat" ) ); // EWL has this file.

			// other files
			var filesFolderInInstallationPath =
				EwlStatics.CombinePaths(
					InstallationFileStatics.GetGeneralFilesFolderPath( installation.GeneralLogic.Path, true ),
					InstallationFileStatics.FilesFolderName );
			if( Directory.Exists( filesFolderInInstallationPath ) )
				IoMethods.CopyFolder( filesFolderInInstallationPath, EwlStatics.CombinePaths( folderPath, InstallationFileStatics.FilesFolderName ), false );
		}

		private static void writeNuGetPackageManifest( DevelopmentInstallation installation, bool? prerelease, DateTime? localExportDateAndTime, TextWriter writer ) {
			writer.WriteLine( "<?xml version=\"1.0\"?>" );
			writer.WriteLine( "<package>" );
			writer.WriteLine( "<metadata>" );
			writer.WriteLine(
				"<id>" + EwlNuGetPackageSpecificationStatics.GetNuGetPackageId( installation.ExistingInstallationLogic.RuntimeConfiguration.SystemShortName ) + "</id>" );
			writer.WriteLine(
				"<version>" +
				EwlNuGetPackageSpecificationStatics.GetNuGetPackageVersionString(
					installation.CurrentMajorVersion,
					!prerelease.HasValue || prerelease.Value ? installation.NextBuildNumber as int? : null,
					localExportDateAndTime: localExportDateAndTime ) + "</version>" );
			writer.WriteLine( "<title>" + installation.ExistingInstallationLogic.RuntimeConfiguration.SystemName + "</title>" );
			writer.WriteLine( "<authors>William Gross, Greg Smalter, Sam Rueby</authors>" );
			writer.WriteLine(
				"<description>The {0} ({1}) is an extremely opinionated library for web applications that trades off performance, scalability, and development flexibility for an ease of maintenance you won't find anywhere else.</description>"
					.FormatWith( EwlStatics.EwlName, EwlStatics.EwlInitialism ) );
			writer.WriteLine( "<projectUrl>http://enterpriseweblibrary.org</projectUrl>" );
			writer.WriteLine( "<licenseUrl>http://opensource.org/licenses/MIT</licenseUrl>" );
			writer.WriteLine( "<requireLicenseAcceptance>false</requireLicenseAcceptance>" );
			writer.WriteLine( "<dependencies>" );

			var lines =
				from line in File.ReadAllLines( EwlStatics.CombinePaths( installation.GeneralLogic.Path, AppStatics.CoreProjectName, "packages.config" ) )
				let trimmedLine = line.Trim()
				where trimmedLine.StartsWith( "<package " )
				select trimmedLine;
			foreach( var line in lines )
				writer.WriteLine( line.Replace( "package", "dependency" ).Replace( " targetFramework=\"net451\"", "" ) );

			writer.WriteLine( "</dependencies>" );
			writer.WriteLine( "<tags>C# ASP.NET DAL SQL-Server MySQL Oracle</tags>" );
			writer.WriteLine( "</metadata>" );
			writer.WriteLine( "</package>" );
		}

		public static Operation Instance { get { return instance; } }
		private ExportLogic() {}

		bool Operation.IsValid( Installation installation ) {
			return installation is DevelopmentInstallation;
		}

		void Operation.Execute( Installation genericInstallation, OperationResult operationResult ) {
			var installation = genericInstallation as DevelopmentInstallation;

			var logicPackagesFolderPath = EwlStatics.CombinePaths( installation.GeneralLogic.Path, "Logic Packages" );
			IoMethods.DeleteFolder( logicPackagesFolderPath );

			// Set up the main (build) object in the build message.
			var build = new RedStapler.StandardLibrary.InstallationSupportUtility.RsisInterface.Messages.BuildMessage.Build();
			build.SystemName = installation.ExistingInstallationLogic.RuntimeConfiguration.SystemName;
			build.SystemShortName = installation.ExistingInstallationLogic.RuntimeConfiguration.SystemShortName;
			build.MajorVersion = installation.CurrentMajorVersion;
			build.BuildNumber = installation.NextBuildNumber;
			build.LogicSize = ConfigurationLogic.NDependIsPresent && !installation.DevelopmentInstallationLogic.SystemIsEwl
				                  ? GetLogicSize.GetNDependLocCount( installation, false ) as int?
				                  : null;
			var serverSideLogicFolderPath = EwlStatics.CombinePaths( logicPackagesFolderPath, "Server Side Logic" );
			packageWebApps( installation, serverSideLogicFolderPath );
			packageWindowsServices( installation, serverSideLogicFolderPath );
			packageServerSideConsoleApps( installation, serverSideLogicFolderPath );
			packageGeneralFiles( installation, serverSideLogicFolderPath, true );
			build.ServerSideLogicPackage = ZipOps.ZipFolderAsByteArray( serverSideLogicFolderPath );
			operationResult.NumberOfBytesTransferred = build.ServerSideLogicPackage.LongLength;

			// Set up the client side application object in the build message, if necessary.
			if( installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject != null ) {
				build.ClientSideApp = new RedStapler.StandardLibrary.InstallationSupportUtility.RsisInterface.Messages.BuildMessage.Build.ClientSideAppType();
				build.ClientSideApp.Name = installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.name;
				build.ClientSideApp.AssemblyName = installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.assemblyName;
				var clientSideAppFolder = EwlStatics.CombinePaths( logicPackagesFolderPath, "Client Side Application" );
				packageClientSideApp( installation, clientSideAppFolder );
				packageGeneralFiles( installation, clientSideAppFolder, false );
				build.ClientSideApp.Package = ZipOps.ZipFolderAsByteArray( clientSideAppFolder );
				operationResult.NumberOfBytesTransferred += build.ClientSideApp.Package.LongLength;
			}

			// Set up the list of installation objects in the build message.
			build.Installations = new RedStapler.StandardLibrary.InstallationSupportUtility.RsisInterface.Messages.BuildMessage.Build.InstallationsType();
			foreach( var installationConfigurationFolderPath in
				Directory.GetDirectories(
					EwlStatics.CombinePaths(
						installation.ExistingInstallationLogic.RuntimeConfiguration.ConfigurationFolderPath,
						InstallationConfiguration.InstallationConfigurationFolderName,
						InstallationConfiguration.InstallationsFolderName ) ) ) {
				if( Path.GetFileName( installationConfigurationFolderPath ) != InstallationConfiguration.DevelopmentInstallationFolderName ) {
					var buildMessageInstallation = new RedStapler.StandardLibrary.InstallationSupportUtility.RsisInterface.Messages.BuildMessage.Installation();

					// Do not perform schema validation since the schema file on disk may not match this version of the ISU.
					var installationConfigurationFile =
						XmlOps.DeserializeFromFile<InstallationStandardConfiguration>(
							EwlStatics.CombinePaths( installationConfigurationFolderPath, InstallationConfiguration.InstallationStandardConfigurationFileName ),
							false );

					buildMessageInstallation.Id = installationConfigurationFile.rsisInstallationId;
					buildMessageInstallation.Name = installationConfigurationFile.installedInstallation.name;
					buildMessageInstallation.ShortName = installationConfigurationFile.installedInstallation.shortName;
					buildMessageInstallation.IsLiveInstallation = installationConfigurationFile.installedInstallation.InstallationTypeConfiguration != null
						                                              ? installationConfigurationFile.installedInstallation.InstallationTypeConfiguration is
						                                                LiveInstallationConfiguration
						                                              : installationConfigurationFile.installedInstallation.IsLiveInstallation;
					buildMessageInstallation.ConfigurationPackage = ZipOps.ZipFolderAsByteArray( installationConfigurationFolderPath );
					build.Installations.Add( buildMessageInstallation );
					operationResult.NumberOfBytesTransferred += buildMessageInstallation.ConfigurationPackage.LongLength;
				}
			}

			if( installation.DevelopmentInstallationLogic.SystemIsEwl )
				build.NuGetPackages = packageEwl( installation, logicPackagesFolderPath );

			var recognizedInstallation = installation as RecognizedDevelopmentInstallation;
			if( recognizedInstallation == null )
				return;

			build.SystemId = recognizedInstallation.KnownSystemLogic.RsisSystem.Id;

			operationResult.TimeSpentWaitingForNetwork = AppTools.ExecuteTimedRegion(
				delegate {
					using( var memoryStream = new MemoryStream() ) {
						// Understand that by doing this, we are not really taking advantage of streaming, but at least it will be easier to do it the right way some day (probably by implementing our own BuildMessageStream)
						XmlOps.SerializeIntoStream( build, memoryStream );
						memoryStream.Position = 0;

						ConfigurationLogic.ExecuteIsuServiceMethod(
							channel => channel.UploadBuild( new BuildUploadMessage { AuthenticationKey = ConfigurationLogic.AuthenticationKey, BuildDocument = memoryStream } ),
							"build upload" );
					}
				} );
		}

		private void packageWebApps( DevelopmentInstallation installation, string serverSideLogicFolderPath ) {
			// NOTE: When packaging web apps, try to find a way to exclude data files. Apparently web deployment projects include these in their output even though
			// they aren't part of the source web projects. NOTE ON NOTE: We don't use WDPs anymore, so maybe we can eliminate this note.
			foreach( var webProject in installation.DevelopmentInstallationLogic.DevelopmentConfiguration.webProjects ?? new WebProject[ 0 ] ) {
				var webAppPath = EwlStatics.CombinePaths( serverSideLogicFolderPath, webProject.name );

				// Pre-compile the web project.
				try {
					EwlStatics.RunProgram(
						EwlStatics.CombinePaths( RuntimeEnvironment.GetRuntimeDirectory(), "aspnet_compiler" ),
						"-v \"/" + webProject.name + ".csproj\" -p \"" + EwlStatics.CombinePaths( installation.GeneralLogic.Path, webProject.name ) + "\" " +
						( webProject.IsUpdateableWhenInstalledSpecified && webProject.IsUpdateableWhenInstalled ? "-u " : "" ) + "-f \"" + webAppPath + "\"",
						"",
						true );
				}
				catch( Exception e ) {
					throw new UserCorrectableException( "ASP.NET pre-compilation failed for web project " + webProject.name + ".", e );
				}
				try {
					EwlStatics.RunProgram(
						EwlStatics.CombinePaths( AppStatics.DotNetToolsFolderPath, "aspnet_merge" ),
						"\"" + webAppPath + "\" -o " + webProject.NamespaceAndAssemblyName + ".Package -a -copyattrs",
						"",
						true );
				}
				catch( Exception e ) {
					throw new UserCorrectableException( "ASP.NET Merge Tool failed for web project " + webProject.name + ".", e );
				}

				// Delete files and folders that aren't necessary for installed installations.
				IoMethods.DeleteFolder( EwlStatics.CombinePaths( webAppPath, "Generated Code" ) );
				IoMethods.DeleteFolder( EwlStatics.CombinePaths( webAppPath, "obj" ) );
				IoMethods.DeleteFile( EwlStatics.CombinePaths( webAppPath, webProject.name + ".csproj" ) );
				IoMethods.DeleteFile( EwlStatics.CombinePaths( webAppPath, webProject.name + ".csproj.user" ) );
				IoMethods.DeleteFile( EwlStatics.CombinePaths( webAppPath, webProject.name + ".csproj.vspscc" ) );
				IoMethods.DeleteFile( EwlStatics.CombinePaths( webAppPath, AppStatics.StandardLibraryFilesFileName ) );

				var webConfigPath = EwlStatics.CombinePaths( webAppPath, "Web.config" );
				File.WriteAllText(
					webConfigPath,
					File.ReadAllText( webConfigPath )
						.Replace( "debug=\"true\"", "debug=\"false\"" )
						.Replace( "<!--<add name=\"HttpCacheModule\" />-->", "<add name=\"HttpCacheModule\" />" ) );
			}
		}

		private void packageWindowsServices( DevelopmentInstallation installation, string serverSideLogicFolderPath ) {
			foreach( var service in installation.ExistingInstallationLogic.RuntimeConfiguration.WindowsServices ) {
				IoMethods.CopyFolder(
					installation.ExistingInstallationLogic.GetWindowsServiceFolderPath( service, false ),
					EwlStatics.CombinePaths( serverSideLogicFolderPath, service.Name ),
					false );
			}
		}

		private void packageServerSideConsoleApps( DevelopmentInstallation installation, string serverSideLogicFolderPath ) {
			foreach( var project in installation.DevelopmentInstallationLogic.DevelopmentConfiguration.ServerSideConsoleProjectsNonNullable )
				copyServerSideProject( installation, serverSideLogicFolderPath, project.Name );

			// Always copy special projects.
			var testRunnerFolder = EwlStatics.CombinePaths( installation.GeneralLogic.Path, EwlStatics.TestRunnerProjectName );
			if( Directory.Exists( testRunnerFolder ) )
				copyServerSideProject( installation, serverSideLogicFolderPath, EwlStatics.TestRunnerProjectName );
		}

		private void copyServerSideProject( DevelopmentInstallation installation, string serverSideLogicFolderPath, string project ) {
			IoMethods.CopyFolder(
				EwlStatics.CombinePaths( installation.GeneralLogic.Path, project, EwlStatics.GetProjectOutputFolderPath( false ) ),
				EwlStatics.CombinePaths( serverSideLogicFolderPath, project ),
				false );
		}

		private void packageClientSideApp( DevelopmentInstallation installation, string clientSideAppFolder ) {
			IoMethods.CopyFolder(
				EwlStatics.CombinePaths(
					installation.GeneralLogic.Path,
					installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.name,
					EwlStatics.GetProjectOutputFolderPath( false ) ),
				EwlStatics.CombinePaths( clientSideAppFolder, installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.name ),
				false );
		}

		private RedStapler.StandardLibrary.InstallationSupportUtility.RsisInterface.Messages.BuildMessage.Build.NuGetPackagesType packageEwl(
			DevelopmentInstallation installation, string logicPackagesFolderPath ) {
			var buildMessageNuGetPackages = new RedStapler.StandardLibrary.InstallationSupportUtility.RsisInterface.Messages.BuildMessage.Build.NuGetPackagesType();
			buildMessageNuGetPackages.Prerelease = CreateEwlNuGetPackage( installation, false, logicPackagesFolderPath, true );
			buildMessageNuGetPackages.Stable = CreateEwlNuGetPackage( installation, false, logicPackagesFolderPath, false );
			return buildMessageNuGetPackages;
		}
	}
}