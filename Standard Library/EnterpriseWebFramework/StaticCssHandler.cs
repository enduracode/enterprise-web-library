using System;
using System.IO;
using System.Linq;
using System.Web;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// ISU and internal use only.
	/// </summary>
	public class StaticCssHandler: IHttpHandler {
		private const string urlVersionStringPrefix = "-";

		/// <summary>
		/// Development Utility use only.
		/// </summary>
		public static string GetUrlVersionString( DateTimeOffset dateAndTime ) {
			return urlVersionStringPrefix + EwfSafeResponseWriter.GetMinuteResolutionUrlVersionString( dateAndTime );
		}

		/// <summary>
		/// ISU and internal use only.
		/// </summary>
		public static string CombineNamespacesAndProcessEwfIfNecessary( string appNamespace, string appRelativeNamespace ) {
			if( appRelativeNamespace.StartsWith( "Ewf." ) )
				return StandardLibraryMethods.EwfFolderBaseNamespace + "." + appNamespace + "." + appRelativeNamespace.Substring( 4 );

			// App-relative namespace can be empty when this method is called from the ISU.
			return appNamespace + appRelativeNamespace.PrependDelimiter( "." );
		}

		void IHttpHandler.ProcessRequest( HttpContext context ) {
			var url = EwfApp.GetRequestAppRelativeUrl( context.Request );

			// We assume that all URL version strings will have the same length as the format string.
			var prefixedVersionStringIndex = url.LastIndexOf( "." ) -
			                                 ( urlVersionStringPrefix.Length + EwfSafeResponseWriter.MinuteResolutionUrlVersionStringFormat.Length );

			if( prefixedVersionStringIndex < 0 )
				throw new ResourceNotAvailableException( "Failed to find the version and extension in the URL.", null );
			var cssInfo =
				EwfApp.GlobalType.Assembly.CreateInstance(
					CombineNamespacesAndProcessEwfIfNecessary(
						EwfApp.GlobalType.Namespace,
						url.Remove( prefixedVersionStringIndex ).Separate( "/", false ).Select( StandardLibraryMethods.GetCSharpIdentifier ).Aggregate( ( a, b ) => a + "." + b ) +
						"+Info" ) ) as StaticCssInfo;
			if( cssInfo == null )
				throw new ResourceNotAvailableException( "Failed to create an Info object for the request.", null );
			var urlVersionString = url.Substring(
				prefixedVersionStringIndex + urlVersionStringPrefix.Length,
				EwfSafeResponseWriter.MinuteResolutionUrlVersionStringFormat.Length );
			if( EwfSafeResponseWriter.GetMinuteResolutionUrlVersionString( cssInfo.GetResourceLastModificationDateAndTime() ) != urlVersionString )
				throw new ResourceNotAvailableException( "The URL version string does not match the last-modification date/time of the resource.", null );

			new EwfSafeResponseWriter(
				() => File.ReadAllText( cssInfo.FilePath ),
				urlVersionString,
				() => new ResponseMemoryCachingSetup( cssInfo.GetUrl( false, false, false ), cssInfo.GetResourceLastModificationDateAndTime() ) ).WriteResponse();
		}

		bool IHttpHandler.IsReusable { get { return true; } }
	}

	[ Obsolete( "Guaranteed through 31 December 2014. Please use the StaticCssHandler class instead." ) ]
	public class CssHandler {
		[ Obsolete( "Guaranteed through 31 December 2014. Please use the StaticCssHandler class instead." ) ]
		public static string GetUrlVersionString( DateTimeOffset dateAndTime ) {
			return StaticCssHandler.GetUrlVersionString( dateAndTime );
		}
	}
}