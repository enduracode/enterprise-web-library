using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using EnterpriseWebLibrary.IO;
using MimeTypes;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// ISU and internal use only.
	/// </summary>
	public class StaticFileHandler: IHttpHandler {
		/// <summary>
		/// ISU and internal use only.
		/// </summary>
		public const string EwfFolderName = "Ewf";

		/// <summary>
		/// ISU and internal use only.
		/// </summary>
		public const string VersionedFilesFolderName = "VersionedStaticFiles";

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
			var ewfNamespace = EwlStatics.EwfFolderBaseNamespace + "." + appNamespace;
			if( appRelativeNamespace == EwfFolderName )
				return ewfNamespace;
			if( appRelativeNamespace.StartsWith( EwfFolderName + "." ) )
				return ewfNamespace + "." + appRelativeNamespace.Substring( 4 );

			// App-relative namespace can be empty when this method is called from the ISU.
			return appNamespace + appRelativeNamespace.PrependDelimiter( "." );
		}

		void IHttpHandler.ProcessRequest( HttpContext context ) {
			var url = EwfApp.GetRequestAppRelativeUrl( context.Request );

			var queryIndex = url.IndexOf( "?", StringComparison.Ordinal );
			if( queryIndex >= 0 )
				url = url.Remove( queryIndex );

			var extensionIndex = url.LastIndexOf( ".", StringComparison.Ordinal );

			var versionStringOrFileExtensionIndex = extensionIndex;
			var urlVersionString = "";
			if( url.StartsWith( VersionedFilesFolderName + "/" ) || url.StartsWith( EwfFolderName + "/" + VersionedFilesFolderName + "/" ) )
				urlVersionString = "invariant";
			else {
				// We assume that all URL version strings will have the same length as the format string.
				var prefixedVersionStringIndex = extensionIndex - ( urlVersionStringPrefix.Length + EwfSafeResponseWriter.UrlVersionStringFormat.Length );

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
			}

			if( versionStringOrFileExtensionIndex < 0 )
				throw new ResourceNotAvailableException( "Failed to find the extension in the URL.", null );
			var extension = url.Substring( extensionIndex );
			var staticFileInfo =
				EwfApp.GlobalType.Assembly.CreateInstance(
					CombineNamespacesAndProcessEwfIfNecessary(
						EwfApp.GlobalType.Namespace,
						( url.Remove( versionStringOrFileExtensionIndex ) + extension.CapitalizeString() ).Separate( "/", false )
							.Select( EwlStatics.GetCSharpIdentifier )
							.Aggregate( ( a, b ) => a + "." + b ) + "+Info" ) ) as StaticFileInfo;
			if( staticFileInfo == null )
				throw new ResourceNotAvailableException( "Failed to create an Info object for the request.", null );

			var mediaTypeOverride = EwfApp.Instance.GetMediaTypeOverrides().SingleOrDefault( i => i.FileExtension == extension );
			var contentType = mediaTypeOverride != null ? mediaTypeOverride.MediaType : MimeTypeMap.GetMimeType( extension );

			Func<string> cacheKeyGetter = () => staticFileInfo.GetUrl( false, false, false );
			EwfSafeResponseWriter responseWriter;
			if( contentType == ContentTypes.Css ) {
				Func<string> cssGetter = () => File.ReadAllText( staticFileInfo.FilePath );
				responseWriter = urlVersionString.Any()
					                 ? new EwfSafeResponseWriter(
						                   cssGetter,
						                   urlVersionString,
						                   () => new ResponseMemoryCachingSetup( cacheKeyGetter(), staticFileInfo.GetResourceLastModificationDateAndTime() ) )
					                 : new EwfSafeResponseWriter(
						                   () => new EwfResponse( ContentTypes.Css, new EwfResponseBodyCreator( () => CssPreprocessor.TransformCssFile( cssGetter() ) ) ),
						                   staticFileInfo.GetResourceLastModificationDateAndTime(),
						                   memoryCacheKeyGetter: cacheKeyGetter );
			}
			else {
				Func<EwfResponse> responseCreator = () => new EwfResponse(
					                                          contentType,
					                                          new EwfResponseBodyCreator(
					                                          responseStream => {
						                                          using( var fileStream = File.OpenRead( staticFileInfo.FilePath ) )
							                                          IoMethods.CopyStream( fileStream, responseStream );
					                                          } ) );
				responseWriter = urlVersionString.Any()
					                 ? new EwfSafeResponseWriter(
						                   responseCreator,
						                   urlVersionString,
						                   memoryCachingSetupGetter:
						                   () => new ResponseMemoryCachingSetup( cacheKeyGetter(), staticFileInfo.GetResourceLastModificationDateAndTime() ) )
					                 : new EwfSafeResponseWriter(
						                   responseCreator,
						                   staticFileInfo.GetResourceLastModificationDateAndTime(),
						                   memoryCacheKeyGetter: cacheKeyGetter );
			}
			responseWriter.WriteResponse();
		}

		bool IHttpHandler.IsReusable { get { return true; } }
	}

	[ Obsolete( "Guaranteed through 31 December 2014. Please use the StaticFileHandler class instead." ) ]
	public class StaticCssHandler {
		[ Obsolete( "Guaranteed through 31 December 2014. Please use the StaticFileHandler class instead." ) ]
		public static string GetUrlVersionString( DateTimeOffset dateAndTime ) {
			return StaticFileHandler.GetUrlVersionString( dateAndTime );
		}
	}

	[ Obsolete( "Guaranteed through 31 December 2014. Please use the StaticFileHandler class instead." ) ]
	public class CssHandler {
		[ Obsolete( "Guaranteed through 31 December 2014. Please use the StaticFileHandler class instead." ) ]
		public static string GetUrlVersionString( DateTimeOffset dateAndTime ) {
			return StaticFileHandler.GetUrlVersionString( dateAndTime );
		}
	}
}