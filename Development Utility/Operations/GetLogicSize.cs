using System;
using System.Collections.Generic;
using System.IO;
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

		internal static int GetNDependLocCount( DevelopmentInstallation installation, bool debug ) {
			var servicesProvider = new NDependServicesProvider();
			var projectManager = servicesProvider.ProjectManager;
			var project =
				projectManager.CreateTemporaryProject( getAssemblyPaths( installation, debug ).Select( i => Path.GetFullPath( i ).ToAbsoluteFilePath() ).ToArray(),
					TemporaryProjectMode.Temporary );

			StatusStatics.SetStatus( "Performing NDepend analysis." );
			var analysisResult = project.RunAnalysis();
			StatusStatics.SetStatus( "Performed NDepend analysis." );

			var codeBase = analysisResult.CodeBase;
			var generatedCodeAttribute = codeBase.Types.WithFullName( "System.CodeDom.Compiler.GeneratedCodeAttribute" ).SingleOrDefault();
			var methods = from n in codeBase.Application.Namespaces
			              where !n.Name.StartsWith( StandardLibraryMethods.EwfFolderBaseNamespace )
			              from t in n.ChildTypes
			              where generatedCodeAttribute == null || !t.HasAttribute( generatedCodeAttribute )
			              from m in t.MethodsAndContructors
			              where generatedCodeAttribute == null || !m.HasAttribute( generatedCodeAttribute )
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
				         select StandardLibraryMethods.CombinePaths( installation.GeneralLogic.Path, i.name, "bin", i.NamespaceAndAssemblyName + ".dll" ) )
				.Concat( from i in installation.ExistingInstallationLogic.RuntimeConfiguration.WindowsServices
				         select
					         StandardLibraryMethods.CombinePaths( installation.ExistingInstallationLogic.GetWindowsServiceFolderPath( i, debug ),
						         i.NamespaceAndAssemblyName + ".exe" ) )
				.Concat( from i in installation.DevelopmentInstallationLogic.DevelopmentConfiguration.serverSideConsoleProjects ?? new ServerSideConsoleProject[ 0 ]
				         select
					         StandardLibraryMethods.CombinePaths( installation.GeneralLogic.Path,
						         i.Name,
						         StandardLibraryMethods.GetProjectOutputFolderPath( debug ),
						         i.NamespaceAndAssemblyName + ".exe" ) )
				.Concat( installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject != null
					         ? StandardLibraryMethods.CombinePaths( installation.GeneralLogic.Path,
						         installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.name,
						         StandardLibraryMethods.GetProjectOutputFolderPath( debug ),
						         installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.assemblyName + ".exe" ).ToSingleElementArray()
					         : new string[ 0 ] );
		}

		public static Operation Instance { get { return instance; } }
		private GetLogicSize() {}

		bool Operation.IsValid( Installation genericInstallation ) {
			var installation = genericInstallation as DevelopmentInstallation;
			return installation != null && !installation.DevelopmentInstallationLogic.SystemIsEwl;
		}

		void Operation.Execute( Installation genericInstallation, OperationResult operationResult ) {
			if( !ConfigurationLogic.NDependIsPresent )
				throw new UserCorrectableException( "NDepend is not present." );
			var installation = genericInstallation as DevelopmentInstallation;
			var locCount = GetNDependLocCount( installation, true );

			Console.WriteLine();
			Console.WriteLine( "LOGIC SIZE (in size points)" );
			Console.WriteLine( locCount );
			Console.WriteLine();
		}
	}
}