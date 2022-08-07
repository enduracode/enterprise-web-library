using EnterpriseWebLibrary.Configuration;
using Humanizer;
using MimeTypes;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A static file in a web application.
	/// </summary>
	public abstract class StaticFile: ResourceBase {
		/// <summary>
		/// Development Utility and private use only.
		/// </summary>
		public const string FrameworkStaticFilesSourceFolderPath = @"EnterpriseWebFramework\StaticFiles";

		/// <summary>
		/// Development Utility and private use only.
		/// </summary>
		public const string AppStaticFilesFolderName = "StaticFiles";

		private readonly bool isVersioned;

		protected StaticFile( bool isVersioned ) {
			this.isVersioned = isVersioned;
		}

		/// <summary>
		/// EWL use only.
		/// </summary>
		public override string ResourceName => "";

		protected internal override bool IsIntermediateInstallationPublicResource => isFrameworkFile;

		protected sealed override UrlHandler getUrlParent() => EsAsBaseType;

		protected sealed override IEnumerable<UrlPattern> getChildUrlPatterns() => base.getChildUrlPatterns();

		/// <summary>
		/// Gets the last-modification date/time of the resource.
		/// </summary>
		public DateTimeOffset GetResourceLastModificationDateAndTime() {
			// The build date/time is an important factor here. Exclusively using the last write time of the file would prevent re-downloading when we change the
			// expansion of a CSS element without changing the source file. And for non-development installations, we don't use the last write time at all because
			// it's probably much slower (the build date/time is just a literal) and also because we don't expect files to be modified on servers.
			if( ConfigurationStatics.IsDevelopmentInstallation ) {
				var lastWriteTime = File.GetLastWriteTimeUtc( filePath );
				if( lastWriteTime > getBuildDateAndTime() )
					return lastWriteTime;
			}
			return getBuildDateAndTime();
		}

		protected abstract DateTimeOffset getBuildDateAndTime();

		/// <summary>
		/// Gets the path of the file.
		/// </summary>
		private string filePath =>
			EwlStatics.CombinePaths(
				isFrameworkFile
					? ConfigurationStatics.InstallationConfiguration.SystemIsEwl && ConfigurationStatics.IsDevelopmentInstallation
						  ? EwlStatics.CombinePaths(
							  ConfigurationStatics.InstallationConfiguration.InstallationPath,
							  EwlStatics.CoreProjectName,
							  FrameworkStaticFilesSourceFolderPath )
						  : EwlStatics.CombinePaths(
							  ConfigurationStatics.InstallationConfiguration.InstallationPath,
							  InstallationFileStatics.WebFrameworkStaticFilesFolderName )
					: EwlStatics.CombinePaths( EwfConfigurationStatics.AppConfiguration.Path, AppStaticFilesFolderName ),
				relativeFilePath );

		/// <summary>
		/// Gets whether the file is part of the framework.
		/// </summary>
		protected abstract bool isFrameworkFile { get; }

		/// <summary>
		/// Gets the relative path of the file.
		/// </summary>
		protected abstract string relativeFilePath { get; }

		/// <summary>
		/// Framework use only.
		/// </summary>
		protected string getUrlVersionString() => isVersioned ? EwfSafeResponseWriter.GetUrlVersionString( GetResourceLastModificationDateAndTime() ) : "";

		protected sealed override bool disablesUrlNormalization => base.disablesUrlNormalization;

		protected sealed override ExternalRedirect getRedirect() => base.getRedirect();

		protected sealed override EwfSafeRequestHandler getOrHead() {
			var extensionIndex = relativeFilePath.LastIndexOf( '.' );
			if( extensionIndex < 0 )
				throw new ResourceNotAvailableException( "Failed to find the extension in the file path.", null );
			var extension = relativeFilePath.Substring( extensionIndex );

			var mediaTypeOverride = EwfApp.Instance.GetMediaTypeOverrides().SingleOrDefault( i => i.FileExtension == extension );
			var contentType = mediaTypeOverride != null ? mediaTypeOverride.MediaType : MimeTypeMap.GetMimeType( extension );

			var urlVersionString = isVersioned ? "invariant" : "";
			string getCacheKey() => "staticFile-{0}-{1}".FormatWith( isFrameworkFile, relativeFilePath );
			EwfSafeResponseWriter responseWriter;
			if( contentType == TewlContrib.ContentTypes.Css ) {
				Func<string> cssGetter = () => File.ReadAllText( filePath );
				responseWriter = urlVersionString.Any()
					                 ? new EwfSafeResponseWriter(
						                 cssGetter,
						                 urlVersionString,
						                 () => new ResponseMemoryCachingSetup( getCacheKey(), GetResourceLastModificationDateAndTime() ) )
					                 : new EwfSafeResponseWriter(
						                 () => EwfResponse.Create(
							                 TewlContrib.ContentTypes.Css,
							                 new EwfResponseBodyCreator( () => CssPreprocessor.TransformCssFile( cssGetter() ) ) ),
						                 GetResourceLastModificationDateAndTime(),
						                 memoryCacheKeyGetter: getCacheKey );
			}
			else {
				Func<EwfResponse> responseCreator = () => EwfResponse.Create(
					contentType,
					new EwfResponseBodyCreator(
						responseStream => {
							using( var fileStream = File.OpenRead( filePath ) )
								fileStream.CopyTo( responseStream );
						} ) );
				responseWriter = urlVersionString.Any()
					                 ? new EwfSafeResponseWriter(
						                 responseCreator,
						                 urlVersionString,
						                 memoryCachingSetupGetter: () => new ResponseMemoryCachingSetup( getCacheKey(), GetResourceLastModificationDateAndTime() ) )
					                 : new EwfSafeResponseWriter( responseCreator, GetResourceLastModificationDateAndTime(), memoryCacheKeyGetter: getCacheKey );
			}
			return responseWriter;
		}

		protected sealed override bool managesDataAccessCacheInUnsafeRequestMethods => base.managesDataAccessCacheInUnsafeRequestMethods;
		protected sealed override EwfResponse put() => base.put();
		protected sealed override EwfResponse patch() => base.patch();
		protected sealed override EwfResponse delete() => base.delete();
		protected sealed override EwfResponse post() => base.post();

		public sealed override bool MatchesCurrent() => base.MatchesCurrent();

		protected internal sealed override ResourceBase ReCreate() => this;

		public sealed override bool Equals( BasicUrlHandler other ) =>
			other is StaticFile otherFile && otherFile.isFrameworkFile == isFrameworkFile && otherFile.relativeFilePath == relativeFilePath &&
			otherFile.isVersioned == isVersioned;

		public sealed override int GetHashCode() => ( isFrameworkFile, relativeFilePath, isVersioned ).GetHashCode();
	}
}