using System;
using System.IO;
using Humanizer;

namespace EnterpriseWebLibrary.IO {
	/// <summary>
	/// Helps communicate with standard out, standard error, and files.
	/// </summary>
	public class Output {
		/// <summary>
		/// Permanently redirects standard output and error to file, with autoflushing enabled.
		/// </summary>
		public static void RedirectOutputToFile( string outputFileName, string errorFileName ) {
			var outputWriter = new StreamWriter( outputFileName, true );
			var errorWriter = new StreamWriter( errorFileName, true );
			outputWriter.AutoFlush = true;
			errorWriter.AutoFlush = true;
			Console.SetOut( outputWriter );
			Console.SetError( errorWriter );
		}

		/// <summary>
		/// Writes the message prepended by a timestamp.
		/// </summary>
		public static void WriteTimeStampedOutput( string message ) {
			Console.Out.WriteLine( "{0}  {1}".FormatWith( getTimestamp(), message ) );
		}

		/// <summary>
		/// Writes the error message prepended by a timestamp.
		/// </summary>
		public static void WriteTimeStampedError( string message ) {
			Console.Error.WriteLine( "{0}  {1}".FormatWith( getTimestamp(), message ) );
		}

		private static string getTimestamp() {
			var now = DateTime.Now;
			return "{0}, {1}".FormatWith( now.ToDayMonthYearString( true ), now.ToString( "HH:mm:ss", Cultures.EnglishUnitedStates ) );
		}
	}
}