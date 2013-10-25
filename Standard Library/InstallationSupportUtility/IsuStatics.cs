using System;
using Microsoft.Web.Administration;

namespace RedStapler.StandardLibrary.InstallationSupportUtility {
	public class IsuStatics {
		/// <summary>
		/// Standard Library and RSIS use only.
		/// </summary>
		public static void ConfigureIis() {
			// Overlapping commitment of changes to server manager do not end well.
			AppTools.ExecuteAsCriticalRegion( "{1BC5B312-F0F0-11DF-B6B9-118ADFD72085}",
			                                  false,
			                                  delegate {
				                                  try {
					                                  using( var serverManager = new ServerManager() ) {
						                                  var config = serverManager.GetApplicationHostConfiguration();

						                                  var modulesSection = config.GetSection( "system.webServer/modules", "" );
						                                  foreach( var element in modulesSection.GetCollection() )
							                                  element.SetMetadata( "lockItem", null );

						                                  var serverRuntimeSection = config.GetSection( "system.webServer/serverRuntime", "" );
						                                  serverRuntimeSection.OverrideMode = OverrideMode.Allow;

						                                  serverManager.CommitChanges();
					                                  }
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