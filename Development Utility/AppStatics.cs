using System;
using EnterpriseWebLibrary.IO;

namespace EnterpriseWebLibrary.DevelopmentUtility {
	internal static class AppStatics {
		internal const string CoreProjectName = "Core";

		internal const string WebProjectFilesFolderName = "Web Project Files";
		internal const string StandardLibraryFilesFileName = "Standard Library Files.xml";

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