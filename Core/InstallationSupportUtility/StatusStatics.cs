using System;
using RedStapler.StandardLibrary.IO;

namespace RedStapler.StandardLibrary.InstallationSupportUtility {
	public static class StatusStatics {
		public static event Action<string> StatusChanged;

		public static void SetStatus( string status ) {
			Output.WriteTimeStampedOutput( status );

			// According to .NET framework event guidelines (http://msdn.microsoft.com/en-us/library/w369ty8x.aspx), you are supposed to cache an event before raising
			// it in case the last subscriber unsubscribes in another thread between the null check and the raising.
			var sc = StatusChanged;

			if( sc != null )
				sc( status );
		}
	}
}