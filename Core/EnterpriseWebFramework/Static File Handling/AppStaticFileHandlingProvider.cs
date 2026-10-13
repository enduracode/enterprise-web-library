using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// Application-specific logic for static-file handling.
/// </summary>
[ UsedImplicitly( ImplicitUseTargetFlags.WithInheritors ) ]
public class AppStaticFileHandlingProvider {
	/// <summary>
	/// Gets the Internet media type overrides for the application, which are used when serving static files. Do not return null.
	/// </summary>
	protected internal virtual IEnumerable<MediaTypeOverride> GetMediaTypeOverrides() => Enumerable.Empty<MediaTypeOverride>();
}