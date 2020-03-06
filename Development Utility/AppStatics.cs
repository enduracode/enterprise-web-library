using System;
using System.IO;
using System.Linq;
using System.Reflection;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.IO;

namespace EnterpriseWebLibrary.DevelopmentUtility {
	internal static class AppStatics {
		internal const string MercurialRepositoryFolderName = ".hg";

		internal const string CoreProjectName = "Core";

		internal const string WebProjectFilesFolderName = "Web Project Files";
		internal const string StandardLibraryFilesFileName = "Standard Library Files.xml";

		internal static bool NDependIsPresent;

		internal static void Init() {
			NDependIsPresent = ConfigurationStatics.MachineConfiguration != null && Directory.Exists(
				                   EwlStatics.CombinePaths(
					                   Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ),
					                   ConfigurationStatics.MachineConfiguration.NDependFolderPathInUserProfileFolderEffective ) );
			if( NDependIsPresent )
				AppDomain.CurrentDomain.AssemblyResolve += ( sender, args ) => {
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

		internal static string GetLiteralDateTimeExpression( DateTimeOffset dateTime ) {
			return "DateTimeOffset.Parse( \"" + dateTime.ToString( "o" ) + "\", null, DateTimeStyles.RoundtripKind )";
		}
	}
}