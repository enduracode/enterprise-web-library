using System;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.InstallationSupportUtility;

namespace EnterpriseWebLibrary.DevelopmentUtility {
	internal static class Program {
		[ MTAThread ]
		private static int Main( string[] args ) {
			AppTools.Init( "Development Utility", true, new GlobalLogic() );
			return AppTools.ExecuteAppWithStandardExceptionHandling( () => {
				StatusStatics.SetStatus( "Hello world!" );
				StatusStatics.SetStatus( "Hello again!" );
			} );
		}
	}
}