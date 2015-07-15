using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Hosting;
using Humanizer;

namespace EnterpriseWebLibrary {
	internal static class IisConfigurationStatics {
		internal static void ConfigureIis( bool iisExpress ) {
			var assembly = getAssembly( iisExpress );
			using( dynamic serverManager = assembly.CreateInstance( "Microsoft.Web.Administration.ServerManager" ) ) {
				var config = serverManager.GetApplicationHostConfiguration();

				var modulesSection = config.GetSection( "system.webServer/modules", "" );
				foreach( var element in modulesSection.GetCollection() )
					element.SetMetadata( "lockItem", null );

				var serverRuntimeSection = config.GetSection( "system.webServer/serverRuntime", "" );
				serverRuntimeSection.OverrideMode = (dynamic)Enum.Parse( assembly.GetType( "Microsoft.Web.Administration.OverrideMode" ), "Allow" );

				serverManager.CommitChanges();
			}
		}

		// Derived from http://blogs.msdn.com/b/carlosag/archive/2011/01/21/get-iis-bindings-at-runtime-without-being-an-administrator.aspx.
		// Since we want this method to work from within web applications, it's important to use WebConfigurationManager instead of ServerManager. See Dominick
		// Baier's post: http://leastprivilege.com/2007/01/09/iis7-configuration-api/.
		internal static string GetFirstBaseUrlForCurrentSite( bool iisExpress ) {
			var assembly = getAssembly( iisExpress );
			var configurationManagerClass = assembly.GetType( "Microsoft.Web.Administration.WebConfigurationManager" );

			var getSectionMethod = configurationManagerClass.GetMethod( "GetSection", new[] { typeof( string ), typeof( string ), typeof( string ) } );
			dynamic sitesSection = getSectionMethod.Invoke( null, new object[] { null, null, "system.applicationHost/sites" } );

			var site = ( (IEnumerable<dynamic>)sitesSection.GetCollection() ).Single( i => (string)i[ "name" ] == HostingEnvironment.SiteName );
			var firstHttpBinding = ( (IEnumerable<dynamic>)site.GetCollection( "bindings" ) ).First( i => ( (string)i[ "protocol" ] ).StartsWith( "http" ) );

			var bindingInfo = ( (string)firstHttpBinding[ "bindingInformation" ] ).Separate( ":", false );
			var ipAddress = bindingInfo[ 0 ]; // Should never be empty; * means All Unassigned.
			var port = bindingInfo[ 1 ]; // never empty
			var host = bindingInfo[ 2 ];

			return
				NetTools.CombineUrls(
					"{0}://{1}:{2}".FormatWith( (string)firstHttpBinding[ "protocol" ], host.Any() ? host : ipAddress != "*" ? ipAddress : "localhost", port ),
					HttpRuntime.AppDomainAppVirtualPath );
		}

		private static Assembly getAssembly( bool iisExpress ) {
			var assemblyName = "Microsoft.Web.Administration, Version=" + ( iisExpress ? "7.9.0.0" : "7.0.0.0" ) + ", Culture=neutral, PublicKeyToken=31bf3856ad364e35";
			return Assembly.Load( assemblyName );
		}
	}
}