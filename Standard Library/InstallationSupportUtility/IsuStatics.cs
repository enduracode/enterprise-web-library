using System;
using System.Reflection;

namespace RedStapler.StandardLibrary.InstallationSupportUtility {
	public class IsuStatics {
		/// <summary>
		/// Standard Library and RSIS use only.
		/// </summary>
		public static void ConfigureIis( bool iisExpress ) {
			var assemblyName = "Microsoft.Web.Administration, Version=" + ( iisExpress ? "7.9.0.0" : "7.0.0.0" ) + ", Culture=neutral, PublicKeyToken=31bf3856ad364e35";
			var assembly = Assembly.Load( assemblyName );

			// Overlapping commitment of changes to server manager do not end well.
			AppTools.ExecuteAsCriticalRegion( "{1BC5B312-F0F0-11DF-B6B9-118ADFD72085}",
			                                  false,
			                                  delegate {
				                                  try {
					                                  using( dynamic serverManager = assembly.CreateInstance( "Microsoft.Web.Administration.ServerManager" ) ) {
						                                  var config = serverManager.GetApplicationHostConfiguration();

						                                  var modulesSection = config.GetSection( "system.webServer/modules", "" );
						                                  foreach( var element in modulesSection.GetCollection() )
							                                  element.SetMetadata( "lockItem", null );

						                                  var serverRuntimeSection = config.GetSection( "system.webServer/serverRuntime", "" );
						                                  serverRuntimeSection.OverrideMode =
							                                  (dynamic)Enum.Parse( assembly.GetType( "Microsoft.Web.Administration.OverrideMode" ), "Allow" );

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