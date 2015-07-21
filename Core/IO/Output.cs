using System;
using System.IO;

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
		/// Writes the message prepended by DateTime.Now.
		/// </summary>
		public static void WriteTimeStampedOutput( string message ) {
			Console.Out.WriteLine( DateTime.Now + ":  " + message );
		}

		/// <summary>
		/// Writes the error message prepended by DateTime.Now.
		/// </summary>
		public static void WriteTimeStampedError( string message ) {
			Console.Error.WriteLine( DateTime.Now + ":  " + message );
		}
	}
}