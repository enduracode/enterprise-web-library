using System.Reflection;
using EnterpriseWebLibrary.Configuration;
using Tewl.IO;

namespace EnterpriseWebLibrary.DevelopmentUtility {
	internal static class AppStatics {
		internal const string MercurialRepositoryFolderName = ".hg";
		internal const string GitRepositoryFolderName = ".git";
		internal const string StaticFileLogicFolderName = "Logic";

		internal static bool NDependIsPresent;

		internal static void Init() {
			NDependIsPresent = ConfigurationStatics.MachineConfiguration != null && Directory.Exists(
				                   EwlStatics.CombinePaths(
					                   Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ),
					                   ConfigurationStatics.MachineConfiguration.NDependFolderPathInUserProfileFolderEffective ) );
			if( NDependIsPresent )
				AppDomain.CurrentDomain.AssemblyResolve += ( _, args ) => {
					var assemblyName = new AssemblyName( args.Name ).Name;
					if( !new[] { "NDepend.API", "NDepend.Core" }.Contains( assemblyName ) )
						return null;
					return Assembly.LoadFrom(
						EwlStatics.CombinePaths(
							Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ),
							ConfigurationStatics.MachineConfiguration.NDependFolderPathInUserProfileFolderEffective,
							"Lib",
							assemblyName + ".dll" ) );
				};
		}

		internal static string DotNetToolsFolderPath =>
			IoMethods.GetFirstExistingFolderPath(
				new[]
					{
						// Ordered by preferred path.
						@"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools",
						@"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.7.2 Tools",
						@"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.2 Tools"
					},
				".NET Tools" );

		internal static string GetLiteralDateTimeExpression( DateTimeOffset dateTime ) =>
			"DateTimeOffset.Parse( \"" + dateTime.ToString( "o" ) + "\", null, DateTimeStyles.RoundtripKind )";

		// see https://stackoverflow.com/a/1793962/35349
		internal static string NormalizeLineEndingsFromXml( string text ) => text.Replace( Environment.NewLine, "\n" ).Replace( "\n", Environment.NewLine );
	}
}