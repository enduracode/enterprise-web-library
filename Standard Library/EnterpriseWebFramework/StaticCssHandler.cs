using System;
using System.Globalization;
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
			return urlVersionStringPrefix + EwfSafeResponseWriter.GetUrlVersionString( dateAndTime );
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
			var extensionIndex = url.LastIndexOf( "." );

			// We assume that all URL version strings will have the same length as the format string.
			var prefixedVersionStringIndex = extensionIndex - ( urlVersionStringPrefix.Length + EwfSafeResponseWriter.UrlVersionStringFormat.Length );

			var versionStringOrFileExtensionIndex = extensionIndex;
			var urlVersionString = "";
			if( prefixedVersionStringIndex >= 0 ) {
				DateTimeOffset dateAndTime;
				var versionString = url.Substring( prefixedVersionStringIndex + urlVersionStringPrefix.Length, EwfSafeResponseWriter.UrlVersionStringFormat.Length );
				if( DateTimeOffset.TryParseExact(
					versionString,
					EwfSafeResponseWriter.UrlVersionStringFormat,
					DateTimeFormatInfo.InvariantInfo,
					DateTimeStyles.None,
					out dateAndTime ) ) {
					versionStringOrFileExtensionIndex = prefixedVersionStringIndex;
					urlVersionString = versionString;
				}
			}

			if( versionStringOrFileExtensionIndex < 0 )
				throw new ResourceNotAvailableException( "Failed to find the extension in the URL.", null );
			var cssInfo =
				EwfApp.GlobalType.Assembly.CreateInstance(
					CombineNamespacesAndProcessEwfIfNecessary(
						EwfApp.GlobalType.Namespace,
						url.Remove( versionStringOrFileExtensionIndex )
							.Separate( "/", false )
							.Select( StandardLibraryMethods.GetCSharpIdentifier )
							.Aggregate( ( a, b ) => a + "." + b ) + "+Info" ) ) as StaticCssInfo;
			if( cssInfo == null )
				throw new ResourceNotAvailableException( "Failed to create an Info object for the request.", null );

			Func<string> cssGetter = () => File.ReadAllText( cssInfo.FilePath );
			Func<string> cacheKeyGetter = () => cssInfo.GetUrl( false, false, false );
			var responseWriter = urlVersionString.Any()
				                     ? new EwfSafeResponseWriter(
					                       cssGetter,
					                       urlVersionString,
					                       () => new ResponseMemoryCachingSetup( cacheKeyGetter(), cssInfo.GetResourceLastModificationDateAndTime() ) )
				                     : new EwfSafeResponseWriter(
					                       () => new EwfResponse( ContentTypes.Css, new EwfResponseBodyCreator( () => CssPreprocessor.TransformCssFile( cssGetter() ) ) ),
					                       cssInfo.GetResourceLastModificationDateAndTime(),
					                       memoryCacheKeyGetter: cacheKeyGetter );
			responseWriter.WriteResponse();
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