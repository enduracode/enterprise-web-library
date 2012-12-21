using System;
using System.Collections.Generic;
using System.Linq;
using NDepend;
using NDepend.Analysis;
using NDepend.CodeModel;
using NDepend.Path;
using NDepend.Project;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.Configuration.SystemDevelopment;
using RedStapler.StandardLibrary.InstallationSupportUtility;
using RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations {
	internal class GetLogicSize: Operation {
		private static readonly Operation instance = new GetLogicSize();

		internal static int? GetNDependLocCount( DevelopmentInstallation installation, bool debug ) {
			if( !ConfigurationLogic.SystemProviderExists || !ConfigurationLogic.SystemProvider.NDependFolderPathInUserProfileFolder.Any() )
				return null;

			var servicesProvider = new NDependServicesProvider();
			var projectManager = servicesProvider.ProjectManager;
			var project = projectManager.CreateTemporaryProject( getAssemblyPaths( installation, debug ).Select( i => i.ToAbsoluteFilePath() ).ToArray(),
			                                                     TemporaryProjectMode.Temporary );

			StatusStatics.SetStatus( "Performing NDepend analysis." );
			var analysisResult = project.RunAnalysis();
			StatusStatics.SetStatus( "Performed NDepend analysis." );

			var codeBase = analysisResult.CodeBase;
			var generatedCodeAttribute = codeBase.Types.WithFullName( "System.CodeDom.Compiler.GeneratedCodeAttribute" ).Single();
			var methods = from t in codeBase.Application.Types
			              where !t.HasAttribute( generatedCodeAttribute )
			              from m in t.MethodsAndContructors
			              where !m.HasAttribute( generatedCodeAttribute )
			              where m.SourceFileDeclAvailable && m.SourceDecls.Any( s => s.SourceFile.FilePath.ParentDirectoryPath.DirectoryName != "Generated Code" )
			              select m;

			return methods.Where( i => i.NbLinesOfCode.HasValue ).Sum( i => Convert.ToInt32( i.NbLinesOfCode.Value ) );
		}

		private static IEnumerable<string> getAssemblyPaths( DevelopmentInstallation installation, bool debug ) {
			return StandardLibraryMethods.CombinePaths( installation.DevelopmentInstallationLogic.LibraryPath,
			                                            StandardLibraryMethods.GetProjectOutputFolderPath( debug ),
			                                            installation.DevelopmentInstallationLogic.DevelopmentConfiguration.LibraryNamespaceAndAssemblyName + ".dll" )
			                             .ToSingleElementArray()
			                             .Concat( from i in installation.DevelopmentInstallationLogic.DevelopmentConfiguration.webProjects ?? new WebProject[ 0 ]
			                                      select
				                                      StandardLibraryMethods.CombinePaths( installation.GeneralLogic.Path,
				                                                                           i.name,
				                                                                           "bin",
				                                                                           i.NamespaceAndAssemblyName + ".dll" ) )
			                             .Concat( from i in installation.ExistingInstallationLogic.RuntimeConfiguration.WindowsServices
			                                      select
				                                      StandardLibraryMethods.CombinePaths( installation.ExistingInstallationLogic.GetWindowsServiceFolderPath( i, debug ),
				                                                                           i.NamespaceAndAssemblyName + ".exe" ) )
			                             .Concat(
				                             from i in
					                             installation.DevelopmentInstallationLogic.DevelopmentConfiguration.serverSideConsoleProjects ??
					                             new ServerSideConsoleProject[ 0 ]
				                             select
					                             StandardLibraryMethods.CombinePaths( installation.GeneralLogic.Path,
					                                                                  i.Name,
					                                                                  StandardLibraryMethods.GetProjectOutputFolderPath( debug ),
					                                                                  i.AssemblyName + ".exe" ) )
			                             .Concat( installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject != null
				                                      ? StandardLibraryMethods.CombinePaths( installation.GeneralLogic.Path,
				                                                                             installation.DevelopmentInstallationLogic.DevelopmentConfiguration
				                                                                                         .clientSideAppProject.name,
				                                                                             StandardLibraryMethods.GetProjectOutputFolderPath( debug ),
				                                                                             installation.DevelopmentInstallationLogic.DevelopmentConfiguration
				                                                                                         .clientSideAppProject.assemblyName + ".exe" )
				                                                              .ToSingleElementArray()
				                                      : new string[ 0 ] );
		}

		public static Operation Instance { get { return instance; } }
		private GetLogicSize() {}

		bool Operation.IsValid( Installation genericInstallation ) {
			var installation = genericInstallation as DevelopmentInstallation;
			return installation != null && !installation.DevelopmentInstallationLogic.SystemIsEwl;
		}

		void Operation.Execute( Installation genericInstallation, OperationResult operationResult ) {
			var installation = genericInstallation as DevelopmentInstallation;
			var locCount = GetNDependLocCount( installation, true );
			if( !locCount.HasValue )
				throw new UserCorrectableException( "NDepend is not present." );

			Console.WriteLine();
			Console.WriteLine( "LOGIC SIZE (in size points)" );
			Console.WriteLine( locCount.Value );
			Console.WriteLine();
		}
	}
}