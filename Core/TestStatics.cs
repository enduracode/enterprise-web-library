using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Humanizer;
using RedStapler.StandardLibrary.IO;
using RedStapler.StandardLibrary.MailMerging;
using RedStapler.StandardLibrary.Validation;

namespace RedStapler.StandardLibrary {
	internal static class TestStatics {
		public static readonly string OutputFolderPath = EwlStatics.CombinePaths(
			Environment.GetFolderPath( Environment.SpecialFolder.DesktopDirectory ),
			"StdLib Test Outputs" );

		internal static readonly string InputTestFilesFolderPath =
			EwlStatics.CombinePaths( Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ), "..", "..", "TestFiles" );

		public static void RunTests() {
			ZipOps.Test();
			Validator.Test();
			PdfOps.Test();
			MergeOps.Test();
		}

		/// <summary>
		/// This will tell people what to look for in the tests.
		/// Outputs a ReadMe file, with each iteration being a line in a item1: item2 format.
		/// </summary>
		internal static void OutputReadme( string outputFolder, IEnumerable<Tuple<string, string>> explanations ) {
			using( var readme = new StreamWriter( EwlStatics.CombinePaths( outputFolder, "ReadMe.txt" ) ) ) {
				readme.WriteLine( "What to look for" );
				readme.WriteLine();

				foreach( var explanation in explanations ) {
					readme.WriteLine( "{0}: {1}".FormatWith( explanation.Item1, explanation.Item2 ) );
					readme.WriteLine();
				}
			}
		}
	}
}