using System;
using System.IO;
using System.Linq;
using System.Web;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.CssHandling {
	/// <summary>
	/// ISU and internal use only.
	/// </summary>
	public class CssHandler: IHttpHandler {
		// We assume that all version strings will have the same length as this format string.
		private const string versionFormatString = "-yyyyMMddHHmm";

		/// <summary>
		/// ISU use only.
		/// </summary>
		public static string GetFileVersionString( DateTime dateAndTime ) {
			return dateAndTime.ToString( versionFormatString );
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
			var url = context.Request.AppRelativeCurrentExecutionFilePath.Substring( NetTools.HomeUrl.Length );
			var removalIndex = url.LastIndexOf( "." ) - versionFormatString.Length;
			if( removalIndex < 0 )
				throw new ResourceNotAvailableException( "Failed to find the version and extension in the URL.", null );
			var cssInfo =
				EwfApp.GlobalType.Assembly.CreateInstance(
					CombineNamespacesAndProcessEwfIfNecessary(
						EwfApp.GlobalType.Namespace,
						url.Remove( removalIndex ).Separate( "/", false ).Select( StandardLibraryMethods.GetCSharpIdentifier ).Aggregate( ( a, b ) => a + "." + b ) + "+Info" ) )
				as CssInfo;
			if( cssInfo == null )
				throw new ResourceNotAvailableException( "Failed to create an Info object for the request.", null );
			if( cssInfo.GetUrl() != context.Request.AppRelativeCurrentExecutionFilePath )
				throw new ResourceNotAvailableException( "The URL does not exactly match the Info object for the request.", null );

			var response = context.Response;
			response.AddFileDependency( cssInfo.FilePath );

			response.ContentType = ContentTypes.Css;
			response.Cache.SetLastModifiedFromFileDependencies();
			response.Cache.SetMaxAge( TimeSpan.FromDays( 365 ) );
			response.Cache.SetCacheability( HttpCacheability.Public );
			response.Cache.SetValidUntilExpires( true );

			// SetMaxAge has no effect without this line. We are not sure why.
			response.Cache.SetSlidingExpiration( true );

			response.Write( CssPreprocessor.TransformCssFile( File.ReadAllText( cssInfo.FilePath ) ) );
		}

		bool IHttpHandler.IsReusable { get { return true; } }
	}
}