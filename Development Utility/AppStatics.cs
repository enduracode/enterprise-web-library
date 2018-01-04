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
			NDependIsPresent = Directory.Exists(
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

		internal static string DotNetToolsFolderPath => IoMethods.GetFirstExistingFolderPath(
			new[]
				{
					// Ordered by preferred path.
					@"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.2 Tools",
					@"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools", @"C:\Program Files (x86)\Microsoft SDKs\Windows\v8.0A\bin\NETFX 4.0 Tools",
					@"C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\Bin\NETFX 4.0 Tools"
				},
			".NET Tools" );

		internal static string GetLiteralDateTimeExpression( DateTimeOffset dateTime ) {
			return "DateTimeOffset.Parse( \"" + dateTime.ToString( "o" ) + "\", null, DateTimeStyles.RoundtripKind )";
		}
	}
}