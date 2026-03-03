using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic;
using EnterpriseWebLibrary.SystemSpecificLogic;
using MimeTypes;
using NodaTime;
using NodaTime.Extensions;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

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

	private static AppStaticFileHandlingProvider provider = null!;

	/// <summary>
	/// Development Utility and private use only.
	/// </summary>
	public static string GetFrameworkStaticFilesFolderPath( InstallationConfiguration installationConfiguration ) =>
		installationConfiguration is { SystemIsEwl: true, InstallationType: InstallationType.Development }
			? EwlStatics.CombinePaths( installationConfiguration.InstallationPath, EwlStatics.CoreProjectName, FrameworkStaticFilesSourceFolderPath )
			: EwlStatics.CombinePaths( installationConfiguration.InstallationPath, InstallationFileStatics.WebFrameworkStaticFilesFolderName );

	internal static void Init( SystemProviderReference<AppStaticFileHandlingProvider> provider ) {
		StaticFile.provider = provider.GetProvider( returnNullIfNotFound: true ) ?? new AppStaticFileHandlingProvider();
	}

	private readonly bool isVersioned;

	protected StaticFile( bool isVersioned ) {
		this.isVersioned = isVersioned;
	}

	protected override string getResourceName() => "";

	protected internal override bool IsIntermediateInstallationPublicResource => isFrameworkFile;

	protected override IReadOnlyCollection<NestedUrl> getNestedUrls() => [ ];

	protected sealed override UrlHandler? getUrlParent() => EsAsBaseType;

	protected sealed override IEnumerable<UrlPattern> getChildUrlPatterns() => base.getChildUrlPatterns();

	/// <summary>
	/// Gets the last-modification time of the resource.
	/// </summary>
	public Instant GetResourceLastModificationTime() {
		// The build date/time is an important factor here. Exclusively using the last write time of the file would prevent re-downloading when we change the
		// expansion of a CSS element without changing the source file. And for non-development installations, we don't use the last write time at all because
		// it's probably much slower (the build date/time is just a literal) and also because we don't expect files to be modified on servers.
		if( ConfigurationStatics.IsDevelopmentInstallation ) {
			var lastWriteTime = File.GetLastWriteTimeUtc( filePath );
			if( lastWriteTime > getBuildDateAndTime() )
				return lastWriteTime.ToInstant();
		}
		return getBuildDateAndTime().ToInstant();
	}

	protected abstract DateTimeOffset getBuildDateAndTime();

	/// <summary>
	/// Gets the path of the file.
	/// </summary>
	private string filePath =>
		EwlStatics.CombinePaths(
			isFrameworkFile
				? GetFrameworkStaticFilesFolderPath( ConfigurationStatics.InstallationConfiguration )
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
	protected string getUrlVersionString() => isVersioned ? EwfSafeResponseWriter.GetUrlVersionString( GetResourceLastModificationTime() ) : "";

	protected sealed override bool disablesUrlNormalization => base.disablesUrlNormalization;

	protected sealed override ExternalRedirect? getRedirect() => base.getRedirect();

	protected sealed override EwfSafeRequestHandler getOrHead() {
		var extensionIndex = relativeFilePath.LastIndexOf( '.' );
		if( extensionIndex < 0 )
			throw new ResourceNotAvailableException( "Failed to find the extension in the file path.", null );
		var extension = relativeFilePath.Substring( extensionIndex );

		var mediaTypeOverride = provider.GetMediaTypeOverrides().SingleOrDefault( i => i.FileExtension == extension );
		var contentType = mediaTypeOverride != null ? mediaTypeOverride.MediaType : MimeTypeMap.GetMimeType( extension );

		var urlVersionString = isVersioned ? "invariant" : "";
		string getCacheKey() => "staticFile-{0}-{1}".FormatWith( isFrameworkFile, relativeFilePath );
		EwfSafeResponseWriter responseWriter;
		if( contentType == ContentTypes.Css ) {
			responseWriter = urlVersionString.Any()
				                 ? new EwfSafeResponseWriter(
					                 getCss,
					                 urlVersionString,
					                 () => new ResponseMemoryCachingSetup( getCacheKey(), GetResourceLastModificationTime() ) )
				                 : new EwfSafeResponseWriter(
					                 () => EwfResponse.Create( ContentTypes.Css, new EwfResponseBodyCreator( () => CssPreprocessor.TransformCssFile( getCss() ) ) ),
					                 GetResourceLastModificationTime(),
					                 memoryCacheKeyGetter: getCacheKey );
			string getCss() => File.ReadAllText( filePath );
		}
		else {
			var responseCreator = () => EwfResponse.Create(
				contentType,
				new EwfResponseBodyCreator( responseStream => {
					using var fileStream = File.OpenRead( filePath );
					fileStream.CopyTo( responseStream );
				} ) );
			responseWriter = urlVersionString.Any()
				                 ? new EwfSafeResponseWriter(
					                 responseCreator,
					                 urlVersionString,
					                 memoryCachingSetupGetter: () => new ResponseMemoryCachingSetup( getCacheKey(), GetResourceLastModificationTime() ) )
				                 : new EwfSafeResponseWriter( responseCreator, GetResourceLastModificationTime(), memoryCacheKeyGetter: getCacheKey );
		}
		return responseWriter;
	}

	protected sealed override bool managesDataModificationsInUnsafeRequestMethods => base.managesDataModificationsInUnsafeRequestMethods;
	protected sealed override EwfResponse? put() => base.put();
	protected sealed override EwfResponse? patch() => base.patch();
	protected sealed override EwfResponse? delete() => base.delete();
	protected sealed override EwfResponse? post() => base.post();

	public sealed override bool MatchesCurrent() => base.MatchesCurrent();

	protected sealed override ResourceBase reCreate() => this;

	public sealed override bool Equals( BasicUrlHandler? other ) =>
		other is StaticFile otherFile && otherFile.isFrameworkFile == isFrameworkFile && otherFile.relativeFilePath == relativeFilePath &&
		otherFile.isVersioned == isVersioned;

	public sealed override int GetHashCode() => ( isFrameworkFile, relativeFilePath, isVersioned ).GetHashCode();
}