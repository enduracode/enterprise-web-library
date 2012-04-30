using System;
using System.IO;

namespace RedStapler.StandardLibrary.InstallationSupportUtility {
	public class UserCorrectableException: ApplicationException {
		internal static Exception CreateSecondaryException( string message, Exception innerException ) {
			if( innerException is UserCorrectableException )
				return new UserCorrectableException( message, innerException );
			return new ApplicationException( message, innerException );
		}

		public UserCorrectableException( string message ): base( message ) {}
		public UserCorrectableException( string message, Exception innerException ): base( message, innerException ) {}

		public override string ToString() {
			using( var sw = new StringWriter() ) {
				writeMessageAndInnerExceptionMessages( sw, this );
				return sw.ToString();
			}
		}

		private static void writeMessageAndInnerExceptionMessages( StringWriter sw, Exception e ) {
			sw.WriteLine( e.Message );
			if( e.InnerException != null ) {
				sw.WriteLine( "---------- Because ----------" );
				writeMessageAndInnerExceptionMessages( sw, e.InnerException );
			}
		}
	}
}