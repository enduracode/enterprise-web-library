using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.Configuration.SystemDevelopment;
using EnterpriseWebLibrary.InstallationSupportUtility;
using EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;
using NDepend;
using NDepend.Analysis;
using NDepend.CodeModel;
using NDepend.Path;
using NDepend.Project;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations;

internal class GetLogicSize: Operation {
	private static readonly Operation instance = new GetLogicSize();

	internal static int GetNDependLocCount( DevelopmentInstallation installation, bool debug ) {
		var servicesProvider = new NDependServicesProvider();
		var projectManager = servicesProvider.ProjectManager;
		var project = projectManager.CreateTemporaryProject(
			getAssemblyPaths( installation, debug ).Select( i => Path.GetFullPath( i ).ToAbsoluteFilePath() ).ToArray(),
			TemporaryProjectMode.Temporary );

		StatusStatics.SetStatus( "Performing NDepend analysis." );
		var analysisResult = project.RunAnalysis();
		StatusStatics.SetStatus( "Performed NDepend analysis." );

		var codeBase = analysisResult.CodeBase;
		var generatedCodeAttribute = codeBase.Types.WithFullName( "System.CodeDom.Compiler.GeneratedCodeAttribute" ).SingleOrDefault();
		var methods = from n in codeBase.Application.Namespaces
		              from t in n.ChildTypes
		              where generatedCodeAttribute == null || !t.HasAttribute( generatedCodeAttribute )
		              from m in t.MethodsAndConstructors
		              where generatedCodeAttribute == null || !m.HasAttribute( generatedCodeAttribute )
		              // We've considered excluding .designer.cs files here, but decided that they should remain part of the count since they still represent
		              // logic that must be maintained (in the designer).
		              where m.SourceFileDeclAvailable && m.SourceDecls.Any( s => s.SourceFile.FilePath.ParentDirectoryPath.DirectoryName != "Generated Code" )
		              select m;

		return methods.Where( i => i.NbLinesOfCode.HasValue ).Sum( i => Convert.ToInt32( i.NbLinesOfCode.Value ) );
	}

	private static IEnumerable<string> getAssemblyPaths( DevelopmentInstallation installation, bool debug ) {
		return EwlStatics
			.CombinePaths(
				installation.DevelopmentInstallationLogic.LibraryPath,
				ConfigurationStatics.GetProjectOutputFolderPath( debug ),
				installation.DevelopmentInstallationLogic.DevelopmentConfiguration.LibraryNamespaceAndAssemblyName + ".dll" )
			.ToCollection()
			.Concat(
				from i in installation.DevelopmentInstallationLogic.DevelopmentConfiguration.webProjects ?? Enumerable.Empty<WebProject>()
				select
					EwlStatics.CombinePaths(
						installation.GeneralLogic.Path,
						i.name,
						ConfigurationStatics.GetProjectOutputFolderPath( debug, runtimeIdentifier: "win10-x64" ),
						i.NamespaceAndAssemblyName + ".dll" ) )
			.Concat(
				from i in installation.ExistingInstallationLogic.RuntimeConfiguration.WindowsServices
				select EwlStatics.CombinePaths( installation.ExistingInstallationLogic.GetWindowsServiceFolderPath( i, debug ), i.NamespaceAndAssemblyName + ".exe" ) )
			.Concat(
				from i in installation.DevelopmentInstallationLogic.DevelopmentConfiguration.ServerSideConsoleProjectsNonNullable
				select EwlStatics.CombinePaths(
					installation.GeneralLogic.Path,
					i.Name,
					ConfigurationStatics.GetProjectOutputFolderPath( debug, runtimeIdentifier: "win10-x64" ),
					i.NamespaceAndAssemblyName + ".exe" ) )
			.Concat(
				installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject != null
					? EwlStatics.CombinePaths(
							installation.GeneralLogic.Path,
							installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.Name,
							ConfigurationStatics.GetProjectOutputFolderPath( debug, runtimeIdentifier: "win10-x64" ),
							installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.NamespaceAndAssemblyName + ".exe" )
						.ToCollection()
					: Enumerable.Empty<string>() );
	}

	public static Operation Instance => instance;
	private GetLogicSize() {}

	bool Operation.IsValid( Installation genericInstallation ) {
		var installation = genericInstallation as DevelopmentInstallation;
		return installation != null && !installation.DevelopmentInstallationLogic.SystemIsEwl;
	}

	void Operation.Execute( Installation genericInstallation, IReadOnlyList<string> arguments, OperationResult operationResult ) {
		if( !AppStatics.NDependIsPresent )
			throw new UserCorrectableException( "NDepend is not present." );
		var installation = genericInstallation as DevelopmentInstallation;
		var locCount = GetNDependLocCount( installation, true );

		Console.WriteLine();
		Console.WriteLine( "LOGIC SIZE (in size points)" );
		Console.WriteLine( locCount );
		Console.WriteLine();
	}
}