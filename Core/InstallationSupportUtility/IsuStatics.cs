using System;

namespace RedStapler.StandardLibrary.InstallationSupportUtility {
	public class IsuStatics {
		/// <summary>
		/// EWL Core and ISU use only.
		/// </summary>
		public static void ConfigureIis( bool iisExpress ) {
			// Overlapping commitment of changes to server manager do not end well.
			AppTools.ExecuteAsCriticalRegion(
				"{1BC5B312-F0F0-11DF-B6B9-118ADFD72085}",
				false,
				delegate {
					try {
						IisConfigurationStatics.ConfigureIis( iisExpress );
					}
					catch( Exception e ) {
						const string message = "Failed to configure IIS.";
						if( e is UnauthorizedAccessException )
							throw new UserCorrectableException( message, e );
						throw new ApplicationException( message, e );
					}
				} );
		}
	}
}