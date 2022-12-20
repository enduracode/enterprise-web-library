using System.Text;
using System.Threading.Tasks;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.Configuration.InstallationStandard;
using EnterpriseWebLibrary.Configuration.SystemDevelopment;
using EnterpriseWebLibrary.DevelopmentUtility.Configuration.Packaging;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.InstallationSupportUtility;
using EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;
using EnterpriseWebLibrary.IO;
using EnterpriseWebLibrary.TewlContrib;
using Tewl.IO;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations {
	internal class ExportLogic: Operation {
		private static readonly Operation instance = new ExportLogic();

		internal static PackagingConfiguration GetPackagingConfiguration( DevelopmentInstallation installation ) {
			var filePath = EwlStatics.CombinePaths(
				installation.ExistingInstallationLogic.RuntimeConfiguration.ConfigurationFolderPath,
				InstallationConfiguration.InstallationConfigurationFolderName,
				InstallationConfiguration.InstallationsFolderName,
				"Packaging" + FileExtensions.Xml );
			return File.Exists( filePath )
				       ? XmlOps.DeserializeFromFile<PackagingConfiguration>( filePath, false )
				       : new PackagingConfiguration
					       {
						       SystemName = installation.ExistingInstallationLogic.RuntimeConfiguration.SystemName,
						       SystemShortName = installation.ExistingInstallationLogic.RuntimeConfiguration.SystemShortName
					       };
		}

		internal static IReadOnlyCollection<( string id, IReadOnlyList<byte[]> packages )> CreateEwlNuGetPackages(
			DevelopmentInstallation installation, PackagingConfiguration packagingConfiguration, bool useDebugAssembly, string outputFolderPath,
			IEnumerable<bool?> prereleaseValues ) {
			var now = DateTime.Now;
			var packages = new List<( string, IReadOnlyList<byte[]> )>();

			var mainId = packagingConfiguration.SystemShortName;
			var mainPackages = prereleaseValues.Select(
					prerelease => {
						var localExportDateAndTime = prerelease.HasValue ? (DateTime?)null : now;

						IoMethods.ExecuteWithTempFolder(
							folderPath => {
								TewlContrib.ProcessTools.RunProgram(
									"dotnet",
									"build \"{0}\" --configuration {1} --no-restore".FormatWith(
										EwlStatics.CombinePaths( installation.GeneralLogic.Path, EwlStatics.CoreProjectName ),
										useDebugAssembly ? "Debug" : "Release" ),
									"",
									true );
								foreach( var fileName in new[] { "dll", "pdb", "xml" }.Select( i => "EnterpriseWebLibrary." + i ) )
									IoMethods.CopyFile(
										EwlStatics.CombinePaths(
											installation.GeneralLogic.Path,
											EwlStatics.CoreProjectName,
											ConfigurationStatics.GetProjectOutputFolderPath( useDebugAssembly ),
											fileName ),
										EwlStatics.CombinePaths( folderPath, @"lib\net6.0-windows7.0", fileName ) );

								var toolsFolderPath = EwlStatics.CombinePaths( folderPath, "tools" );
								IoMethods.CopyFile(
									EwlStatics.CombinePaths( installation.GeneralLogic.Path, @"Development Utility\Package Manager Console Commands.ps1" ),
									EwlStatics.CombinePaths( toolsFolderPath, "init.ps1" ) );

								const string duProjectAndFolderName = "Development Utility";
								publishApp(
									EwlStatics.CombinePaths( installation.GeneralLogic.Path, duProjectAndFolderName ),
									EwlStatics.CombinePaths( toolsFolderPath, duProjectAndFolderName ) );
								packageGeneralFiles( installation, toolsFolderPath, false );
								IoMethods.CopyFolder(
									EwlStatics.CombinePaths(
										installation.ExistingInstallationLogic.RuntimeConfiguration.ConfigurationFolderPath,
										InstallationConfiguration.InstallationConfigurationFolderName,
										InstallationConfiguration.InstallationsFolderName,
										!prerelease.HasValue || prerelease.Value ? "Testing" : "Live" ),
									EwlStatics.CombinePaths(
										toolsFolderPath,
										InstallationConfiguration.ConfigurationFolderName,
										InstallationConfiguration.InstallationConfigurationFolderName ),
									false );
								if( File.Exists( installation.ExistingInstallationLogic.RuntimeConfiguration.InstallationSharedConfigurationFilePath ) )
									IoMethods.CopyFile(
										installation.ExistingInstallationLogic.RuntimeConfiguration.InstallationSharedConfigurationFilePath,
										EwlStatics.CombinePaths(
											toolsFolderPath,
											InstallationConfiguration.ConfigurationFolderName,
											InstallationConfiguration.InstallationConfigurationFolderName,
											InstallationConfiguration.InstallationSharedConfigurationFileName ) );

								IoMethods.CopyFolder(
									EwlStatics.CombinePaths( installation.GeneralLogic.Path, EwlStatics.CoreProjectName, StaticFile.FrameworkStaticFilesSourceFolderPath ),
									EwlStatics.CombinePaths( toolsFolderPath, InstallationFileStatics.WebFrameworkStaticFilesFolderName ),
									false );
								IoMethods.DeleteFolder(
									EwlStatics.CombinePaths( toolsFolderPath, InstallationFileStatics.WebFrameworkStaticFilesFolderName, AppStatics.StaticFileLogicFolderName ) );

								var manifestPath = EwlStatics.CombinePaths( folderPath, "Package.nuspec" );
								using( var writer = IoMethods.GetTextWriterForWrite( manifestPath ) )
									writeNuGetPackageManifest(
										writer,
										installation,
										mainId,
										"",
										w => {
											var lines = from line in File.ReadAllLines(
												            EwlStatics.CombinePaths(
													            installation.GeneralLogic.Path,
													            EwlStatics.CoreProjectName,
													            EwlStatics.CoreProjectName + ".csproj" ) )
											            let trimmedLine = line.Trim()
											            where trimmedLine.StartsWith( "<PackageReference " )
											            select trimmedLine;
											foreach( var line in lines )
												w.WriteLine( line.Replace( "PackageReference", "dependency" ).Replace( "Include", "id" ).Replace( "Version", "version" ) );
										},
										prerelease,
										localExportDateAndTime );

								StatusStatics.SetStatus(
									TewlContrib.ProcessTools.RunProgram(
										EwlStatics.CombinePaths( installation.GeneralLogic.Path, @"Solution Files\nuget" ),
										"pack \"" + manifestPath + "\" -OutputDirectory \"" + outputFolderPath + "\"",
										"",
										true ) );
							} );

						return File.ReadAllBytes(
							EwlStatics.CombinePaths(
								outputFolderPath,
								EwlNuGetPackageSpecificationStatics.GetNuGetPackageFileName(
									mainId,
									installation.CurrentMajorVersion,
									!prerelease.HasValue || prerelease.Value ? installation.NextBuildNumber : null,
									localExportDateAndTime: localExportDateAndTime ) ) );
					} )
				.MaterializeAsList();
			packages.Add( ( mainId, mainPackages ) );

			var mySqlId = mainId + ".MySql";
			packages.Add(
				( mySqlId,
					createProviderNuGetPackages(
						installation,
						mainId,
						"MySQL Provider",
						EwlStatics.MySqlProviderProjectPath,
						"EnterpriseWebLibrary.MySql",
						mySqlId,
						"<dependency id=\"MySql.Data\" version=\"8.0.31\" />".ToCollection(),
						now,
						useDebugAssembly,
						outputFolderPath,
						prereleaseValues ) ) );

			var samlId = mainId + ".Saml";
			packages.Add(
				( samlId,
					createProviderNuGetPackages(
						installation,
						mainId,
						"SAML Provider",
						EwlStatics.SamlProviderProjectPath,
						"EnterpriseWebLibrary.Saml",
						samlId,
						"<dependency id=\"ComponentSpace.Saml2.Licensed\" version=\"4.5.0\" />".ToCollection(),
						now,
						useDebugAssembly,
						outputFolderPath,
						prereleaseValues ) ) );

			return packages;
		}

		private static void publishApp( string projectPath, string outputFolderPath ) {
			TewlContrib.ProcessTools.RunProgram(
				"dotnet",
				"publish \"{0}\" --configuration Release --no-restore --output \"{1}\"".FormatWith( projectPath, outputFolderPath ),
				"",
				true );
		}

		private static IReadOnlyList<byte[]> createProviderNuGetPackages(
			DevelopmentInstallation installation, string mainPackageId, string projectName, string projectPath, string assemblyName, string packageId,
			IEnumerable<string> dependencies, DateTime now, bool useDebugAssembly, string outputFolderPath, IEnumerable<bool?> prereleaseValues ) =>
			prereleaseValues.Select(
					prerelease => {
						var localExportDateAndTime = prerelease.HasValue ? (DateTime?)null : now;

						IoMethods.ExecuteWithTempFolder(
							folderPath => {
								TewlContrib.ProcessTools.RunProgram(
									"dotnet",
									"build \"{0}\" --configuration {1} --no-restore".FormatWith(
										EwlStatics.CombinePaths( installation.GeneralLogic.Path, projectPath ),
										useDebugAssembly ? "Debug" : "Release" ),
									"",
									true );
								foreach( var fileName in new[] { "dll", "pdb" }.Select( i => "{0}.{1}".FormatWith( assemblyName, i ) ) )
									IoMethods.CopyFile(
										EwlStatics.CombinePaths(
											installation.GeneralLogic.Path,
											projectPath,
											ConfigurationStatics.GetProjectOutputFolderPath( useDebugAssembly ),
											fileName ),
										EwlStatics.CombinePaths( folderPath, @"lib\net6.0-windows7.0", fileName ) );

								var manifestPath = EwlStatics.CombinePaths( folderPath, "Package.nuspec" );
								using( var writer = IoMethods.GetTextWriterForWrite( manifestPath ) )
									writeNuGetPackageManifest(
										writer,
										installation,
										packageId,
										projectName,
										w => {
											w.WriteLine(
												"<dependency id=\"{0}\" version=\"[{1}]\" />".FormatWith(
													mainPackageId,
													EwlNuGetPackageSpecificationStatics.GetNuGetPackageVersionString(
														installation.CurrentMajorVersion,
														!prerelease.HasValue || prerelease.Value ? installation.NextBuildNumber : null,
														localExportDateAndTime: localExportDateAndTime ) ) );
											foreach( var i in dependencies )
												w.WriteLine( i );
										},
										prerelease,
										localExportDateAndTime );

								StatusStatics.SetStatus(
									TewlContrib.ProcessTools.RunProgram(
										EwlStatics.CombinePaths( installation.GeneralLogic.Path, @"Solution Files\nuget" ),
										"pack \"" + manifestPath + "\" -OutputDirectory \"" + outputFolderPath + "\"",
										"",
										true ) );
							} );

						return File.ReadAllBytes(
							EwlStatics.CombinePaths(
								outputFolderPath,
								EwlNuGetPackageSpecificationStatics.GetNuGetPackageFileName(
									packageId,
									installation.CurrentMajorVersion,
									!prerelease.HasValue || prerelease.Value ? installation.NextBuildNumber : null,
									localExportDateAndTime: localExportDateAndTime ) ) );
					} )
				.MaterializeAsList();

		private static void packageGeneralFiles( DevelopmentInstallation installation, string folderPath, bool includeDatabaseUpdates ) {
			// configuration files
			var configurationFolderPath = EwlStatics.CombinePaths( folderPath, InstallationConfiguration.ConfigurationFolderName );
			IoMethods.CopyFolder( installation.ExistingInstallationLogic.RuntimeConfiguration.ConfigurationFolderPath, configurationFolderPath, false );
			IoMethods.RecursivelyRemoveReadOnlyAttributeFromItem( configurationFolderPath );
			IoMethods.DeleteFolder( EwlStatics.CombinePaths( configurationFolderPath, InstallationConfiguration.AsposeLicenseFolderName ) );
			IoMethods.DeleteFolder( EwlStatics.CombinePaths( configurationFolderPath, InstallationConfiguration.InstallationConfigurationFolderName ) );
			IoMethods.DeleteFolder( EwlStatics.CombinePaths( configurationFolderPath, ConfigurationStatics.ProvidersFolderAndNamespaceName ) );
			if( !includeDatabaseUpdates )
				IoMethods.DeleteFile( EwlStatics.CombinePaths( configurationFolderPath, ExistingInstallationLogic.SystemDatabaseUpdatesFileName ) );
			IoMethods.DeleteFile( EwlStatics.CombinePaths( configurationFolderPath, InstallationConfiguration.SystemDevelopmentConfigurationFileName ) );

			// other files
			var filesFolderInInstallationPath = EwlStatics.CombinePaths(
				InstallationFileStatics.GetGeneralFilesFolderPath( installation.GeneralLogic.Path, true ),
				InstallationFileStatics.FilesFolderName );
			if( Directory.Exists( filesFolderInInstallationPath ) )
				IoMethods.CopyFolder( filesFolderInInstallationPath, EwlStatics.CombinePaths( folderPath, InstallationFileStatics.FilesFolderName ), false );
		}

		private static void writeNuGetPackageManifest(
			TextWriter writer, DevelopmentInstallation installation, string id, string projectName, Action<TextWriter> dependencyWriter, bool? prerelease,
			DateTime? localExportDateAndTime ) {
			writer.WriteLine( "<?xml version=\"1.0\" encoding=\"utf-8\"?>" );
			writer.WriteLine( "<package xmlns=\"http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd\">" );
			writer.WriteLine( "<metadata>" );
			writer.WriteLine( "<id>" + id + "</id>" );
			writer.WriteLine(
				"<version>" + EwlNuGetPackageSpecificationStatics.GetNuGetPackageVersionString(
					installation.CurrentMajorVersion,
					!prerelease.HasValue || prerelease.Value ? installation.NextBuildNumber : null,
					localExportDateAndTime: localExportDateAndTime ) + "</version>" );
			writer.WriteLine(
				"<title>" + installation.ExistingInstallationLogic.RuntimeConfiguration.SystemName + projectName.PrependDelimiter( " - " ) + "</title>" );
			writer.WriteLine( "<authors>William Gross, Greg Smalter, Sam Rueby</authors>" );
			writer.WriteLine(
				"<description>The {0} ({1}), together with its tailored infrastructure platform, is a highly opinionated foundation for web-based enterprise software.</description>"
					.FormatWith( EwlStatics.EwlName, EwlStatics.EwlInitialism ) );
			writer.WriteLine( "<projectUrl>http://enterpriseweblibrary.org</projectUrl>" );
			writer.WriteLine( "<license type=\"expression\">MIT</license>" );
			writer.WriteLine( "<requireLicenseAcceptance>false</requireLicenseAcceptance>" );
			writer.WriteLine( "<dependencies>" );
			writer.WriteLine( "<group targetFramework=\"net6.0-windows7.0\">" );
			dependencyWriter( writer );
			writer.WriteLine( "</group>" );
			writer.WriteLine( "</dependencies>" );
			writer.WriteLine( "<tags>C# ASP.NET DAL SQL-Server MySQL Oracle</tags>" );
			writer.WriteLine( "</metadata>" );
			writer.WriteLine( "</package>" );
		}

		public static Operation Instance => instance;
		private ExportLogic() {}

		bool Operation.IsValid( Installation installation ) {
			return installation is DevelopmentInstallation;
		}

		void Operation.Execute( Installation genericInstallation, IReadOnlyList<string> arguments, OperationResult operationResult ) {
			var installation = genericInstallation as DevelopmentInstallation;
			var packagingConfiguration = GetPackagingConfiguration( installation );

			var logicPackagesFolderPath = EwlStatics.CombinePaths( installation.GeneralLogic.Path, "Logic Packages" );
			IoMethods.DeleteFolder( logicPackagesFolderPath );

			// Set up the main (build) object in the build message.
			var build = new InstallationSupportUtility.SystemManagerInterface.Messages.BuildMessage.Build();
			build.SystemName = packagingConfiguration.SystemName;
			build.SystemShortName = packagingConfiguration.SystemShortName;
			build.MajorVersion = installation.CurrentMajorVersion;
			build.BuildNumber = installation.NextBuildNumber;

			if( Directory.Exists( EwlStatics.CombinePaths( installation.GeneralLogic.Path, AppStatics.MercurialRepositoryFolderName ) ) ) {
				var hgOutput = TewlContrib.ProcessTools.RunProgram(
						@"C:\Program Files\TortoiseHg\hg",
						"--debug identify --id \"{0}\"".FormatWith( installation.GeneralLogic.Path ),
						"",
						true )
					.Trim();
				build.ChangesetId = hgOutput.Length == 40 ? hgOutput : "";
			}
			else if( Directory.Exists( EwlStatics.CombinePaths( installation.GeneralLogic.Path, AppStatics.GitRepositoryFolderName ) ) )
				build.ChangesetId = TewlContrib.ProcessTools.RunProgram( "git", "rev-parse --verify HEAD", "", true );
			else
				build.ChangesetId = "";
			build.HgChangesetId = "";

			var serverSideLogicFolderPath = EwlStatics.CombinePaths( logicPackagesFolderPath, "Server Side Logic" );
			packageWebApps( installation, serverSideLogicFolderPath );
			packageWindowsServices( installation, serverSideLogicFolderPath );
			packageServerSideConsoleApps( installation, serverSideLogicFolderPath );
			packageGeneralFiles( installation, serverSideLogicFolderPath, true );
			build.ServerSideLogicPackage = ZipOps.ZipFolderAsByteArray( serverSideLogicFolderPath );
			operationResult.NumberOfBytesTransferred = build.ServerSideLogicPackage.LongLength;

			// Set up the client side application object in the build message, if necessary.
			if( installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject != null ) {
				build.ClientSideApp = new InstallationSupportUtility.SystemManagerInterface.Messages.BuildMessage.Build.ClientSideAppType();
				build.ClientSideApp.Name = installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.name;
				build.ClientSideApp.AssemblyName = installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.assemblyName;
				var clientSideAppFolder = EwlStatics.CombinePaths( logicPackagesFolderPath, "Client Side Application" );
				packageClientSideApp( installation, clientSideAppFolder );
				packageGeneralFiles( installation, clientSideAppFolder, false );
				build.ClientSideApp.Package = ZipOps.ZipFolderAsByteArray( clientSideAppFolder );
				operationResult.NumberOfBytesTransferred += build.ClientSideApp.Package.LongLength;
			}

			// We cannot calculate the logic size until after the package… methods above produce build output for all projects.
			build.LogicSize = AppStatics.NDependIsPresent && !installation.DevelopmentInstallationLogic.SystemIsEwl
				                  ? GetLogicSize.GetNDependLocCount( installation, false )
				                  : null;

			// Set up the list of installation objects in the build message.
			build.Installations = new InstallationSupportUtility.SystemManagerInterface.Messages.BuildMessage.Build.InstallationsType();
			foreach( var installationConfigurationFolderPath in Directory.GetDirectories(
				        EwlStatics.CombinePaths(
					        installation.ExistingInstallationLogic.RuntimeConfiguration.ConfigurationFolderPath,
					        InstallationConfiguration.InstallationConfigurationFolderName,
					        InstallationConfiguration.InstallationsFolderName ) ) )
				if( !new[] { InstallationConfiguration.DevelopmentInstallationFolderName, AppStatics.MercurialRepositoryFolderName, AppStatics.GitRepositoryFolderName }
					    .Contains( Path.GetFileName( installationConfigurationFolderPath ) ) ) {
					var buildMessageInstallation = new InstallationSupportUtility.SystemManagerInterface.Messages.BuildMessage.Installation();

					// Do not perform schema validation since the schema file on disk may not match this version of the ISU.
					var installationConfigurationFile = XmlOps.DeserializeFromFile<InstallationStandardConfiguration>(
						EwlStatics.CombinePaths( installationConfigurationFolderPath, InstallationConfiguration.InstallationStandardConfigurationFileName ),
						false );

					buildMessageInstallation.Id = installationConfigurationFile.rsisInstallationId;
					buildMessageInstallation.Name = installationConfigurationFile.installedInstallation.name;
					buildMessageInstallation.ShortName = installationConfigurationFile.installedInstallation.shortName;
					buildMessageInstallation.IsLiveInstallation =
						installationConfigurationFile.installedInstallation.InstallationTypeConfiguration is LiveInstallationConfiguration;

					var packageFolderPath = EwlStatics.CombinePaths(
						logicPackagesFolderPath,
						$"{installationConfigurationFile.installedInstallation.name} Configuration" );
					IoMethods.CopyFolder( installationConfigurationFolderPath, packageFolderPath, false );
					if( File.Exists( installation.ExistingInstallationLogic.RuntimeConfiguration.InstallationSharedConfigurationFilePath ) )
						IoMethods.CopyFile(
							installation.ExistingInstallationLogic.RuntimeConfiguration.InstallationSharedConfigurationFilePath,
							EwlStatics.CombinePaths( packageFolderPath, InstallationConfiguration.InstallationSharedConfigurationFileName ) );
					buildMessageInstallation.ConfigurationPackage = ZipOps.ZipFolderAsByteArray( packageFolderPath );

					build.Installations.Add( buildMessageInstallation );
					operationResult.NumberOfBytesTransferred += buildMessageInstallation.ConfigurationPackage.LongLength;
				}

			build.NuGetPackages = new InstallationSupportUtility.SystemManagerInterface.Messages.BuildMessage.Build.NuGetPackagesType();
			if( installation.DevelopmentInstallationLogic.SystemIsEwl )
				build.NuGetPackages.AddRange( packageEwl( installation, packagingConfiguration, logicPackagesFolderPath ) );

			var recognizedInstallation = installation as RecognizedDevelopmentInstallation;
			if( recognizedInstallation == null )
				return;

			build.SystemId = recognizedInstallation.KnownSystemLogic.RsisSystem.Id;

			operationResult.TimeSpentWaitingForNetwork = EwlStatics.ExecuteTimedRegion(
				() => SystemManagerConnectionStatics.ExecuteActionWithSystemManagerClient(
					"build upload",
					client => Task.Run(
							async () => {
								using var content = HttpClientTools.GetRequestContentFromWriter( stream => XmlOps.SerializeIntoStream( build, stream ) );
								using var response = await client.PostAsync( SystemManagerConnectionStatics.BuildsUrlSegment, content );
								response.EnsureSuccessStatusCode();
							} )
						.Wait(),
					supportLargePayload: true ) );
		}

		private void packageWebApps( DevelopmentInstallation installation, string serverSideLogicFolderPath ) {
			foreach( var webProject in installation.DevelopmentInstallationLogic.DevelopmentConfiguration.webProjects ?? Enumerable.Empty<WebProject>() ) {
				publishApp(
					EwlStatics.CombinePaths( installation.GeneralLogic.Path, webProject.name ),
					EwlStatics.CombinePaths( serverSideLogicFolderPath, webProject.name ) );
				IoMethods.CopyFolder(
					EwlStatics.CombinePaths( installation.GeneralLogic.Path, webProject.name, StaticFile.AppStaticFilesFolderName ),
					EwlStatics.CombinePaths( serverSideLogicFolderPath, webProject.name, StaticFile.AppStaticFilesFolderName ),
					false );
				IoMethods.DeleteFolder(
					EwlStatics.CombinePaths( serverSideLogicFolderPath, webProject.name, StaticFile.AppStaticFilesFolderName, AppStatics.StaticFileLogicFolderName ) );
				File.WriteAllText(
					EwlStatics.CombinePaths( serverSideLogicFolderPath, webProject.name, "web.config" ),
					File.ReadAllText( EwlStatics.CombinePaths( ConfigurationStatics.FilesFolderPath, "Web Project Configuration", "web.config" ) )
						.Replace( "@@AssemblyPath", @".\{0}.exe".FormatWith( webProject.NamespaceAndAssemblyName ) )
						.Replace( "@@InitializationTimeoutSeconds", EwfOps.InitializationTimeoutSeconds.ToString() ),
					Encoding.UTF8 );
			}

			if( installation.DevelopmentInstallationLogic.SystemIsEwl ) {
				IoMethods.CopyFolder(
					EwlStatics.CombinePaths( installation.GeneralLogic.Path, EwlStatics.CoreProjectName, StaticFile.FrameworkStaticFilesSourceFolderPath ),
					EwlStatics.CombinePaths( serverSideLogicFolderPath, InstallationFileStatics.WebFrameworkStaticFilesFolderName ),
					false );
				IoMethods.DeleteFolder(
					EwlStatics.CombinePaths(
						serverSideLogicFolderPath,
						InstallationFileStatics.WebFrameworkStaticFilesFolderName,
						AppStatics.StaticFileLogicFolderName ) );
			}
			else {
				var frameworkStaticFilesFolderPath = EwlStatics.CombinePaths(
					installation.GeneralLogic.Path,
					InstallationFileStatics.WebFrameworkStaticFilesFolderName );
				if( Directory.Exists( frameworkStaticFilesFolderPath ) )
					IoMethods.CopyFolder(
						frameworkStaticFilesFolderPath,
						EwlStatics.CombinePaths( serverSideLogicFolderPath, InstallationFileStatics.WebFrameworkStaticFilesFolderName ),
						false );
			}
		}

		private void packageWindowsServices( DevelopmentInstallation installation, string serverSideLogicFolderPath ) {
			foreach( var service in installation.ExistingInstallationLogic.RuntimeConfiguration.WindowsServices )
				publishApp(
					EwlStatics.CombinePaths( installation.GeneralLogic.Path, service.Name ),
					EwlStatics.CombinePaths( serverSideLogicFolderPath, service.Name ) );
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
			publishApp( EwlStatics.CombinePaths( installation.GeneralLogic.Path, project ), EwlStatics.CombinePaths( serverSideLogicFolderPath, project ) );
		}

		private void packageClientSideApp( DevelopmentInstallation installation, string clientSideAppFolder ) {
			publishApp(
				EwlStatics.CombinePaths( installation.GeneralLogic.Path, installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.name ),
				EwlStatics.CombinePaths( clientSideAppFolder, installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.name ) );
		}

		private IEnumerable<InstallationSupportUtility.SystemManagerInterface.Messages.BuildMessage.NuGetPackage> packageEwl(
			DevelopmentInstallation installation, PackagingConfiguration packagingConfiguration, string logicPackagesFolderPath ) =>
			CreateEwlNuGetPackages( installation, packagingConfiguration, false, logicPackagesFolderPath, new bool?[] { true, false } )
				.Select(
					i => {
						var package = new InstallationSupportUtility.SystemManagerInterface.Messages.BuildMessage.NuGetPackage();
						package.Id = i.id;
						package.Prerelease = i.packages[ 0 ];
						package.Stable = i.packages[ 1 ];
						return package;
					} );
	}
}