using System;
using System.IO;
using System.Linq;
using RedStapler.StandardLibrary;

namespace EnterpriseWebLibrary.DevelopmentUtility {
	internal static class AppStatics {
		internal const string CoreProjectName = "Core";

		internal const string WebProjectFilesFolderName = "Web Project Files";
		internal const string StandardLibraryFilesFileName = "Standard Library Files.xml";

		internal static string DotNetToolsFolderPath {
			get {
				var searchPaths = new[]
					{
						// Ordered by preferred path.
						@"C:\Program Files (x86)\Microsoft SDKs\Windows\v8.0A\bin\NETFX 4.0 Tools", @"C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\Bin\NETFX 4.0 Tools"
					};
				try {
					return searchPaths.First( Directory.Exists );
				}
				catch( InvalidOperationException e ) {
					throw new ApplicationException(
						"Unable to find a valid path to the Windows SDK. The following paths do not exist or are inaccessible: " +
						searchPaths.GetCommaDelimitedStringFromCollection(),
						e );
				}
			}
		}

		internal static string GetLiteralDateTimeExpression( DateTimeOffset dateTime ) {
			return "DateTimeOffset.Parse( \"" + dateTime.ToString( "o" ) + "\", null, DateTimeStyles.RoundtripKind )";
		}
	}
}