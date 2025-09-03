using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.EnterpriseWebFramework.Core;
using EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic;
using EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic.AlternativeResourceModes;
using EnterpriseWebLibrary.SystemSpecificLogic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A parent of a resource or entity setup.
/// </summary>
public interface ResourceParent: UrlHandler, WebItem {
	private static readonly List<SystemProviderReference<AppResourceSerializationProvider>> appSerializationProviderRefs = [ ];

	private static Func<ResourceParent, ( string name, string parameters )?>? frameworkResourceSerializer;
	private static SystemProviderReference<SystemResourceSerializationProvider>? systemSerializationProviderRef;

	internal static void Init(
		Func<ResourceParent, ( string, string )?> frameworkResourceSerializer,
		SystemProviderReference<SystemResourceSerializationProvider> systemSerializationProvider,
		SystemProviderReference<AppResourceSerializationProvider> appSerializationProvider ) {
		ResourceParent.frameworkResourceSerializer = frameworkResourceSerializer;
		systemSerializationProviderRef = systemSerializationProvider;
		appSerializationProviderRefs.Add( appSerializationProvider );
	}

	internal static void AddApplication( SystemProviderReference<AppResourceSerializationProvider> provider ) {
		appSerializationProviderRefs.Add( provider );
	}

	private static SystemResourceSerializationProvider systemSerializationProvider => systemSerializationProviderRef!.GetProvider()!;
	private static IEnumerable<AppResourceSerializationProvider> appSerializationProviders => appSerializationProviderRefs.Select( i => i.GetProvider()! );

	/// <summary>
	/// Gets the parent of this parent, or null if there isn’t one.
	/// </summary>
	ResourceParent? Parent { get; }

	/// <summary>
	/// Gets the name of this parent.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Gets whether this parent is public in intermediate installations, regardless of other authorization logic that may exist.
	/// </summary>
	internal bool IsIntermediateInstallationPublicParent { get; }

	/// <summary>
	/// Returns the log-in page to use for this parent, or null for default behavior.
	/// </summary>
	ResourceBase? GetLogInPage( TrustedUrl returnUrl );

	internal sealed EwfUrl GetEwfUrl( bool ensureUserCanAccess, bool ensureNotDisabled, string? fragmentIdentifier ) {
		try {
			if( ensureUserCanAccess && !UserCanAccess )
				throw new ApplicationException( "The authenticated user cannot access the resource." );
			if( ensureNotDisabled && AlternativeMode is DisabledResourceMode )
				throw new ApplicationException( "The resource is disabled." );

			var url = UrlHandlingStatics.GetCanonicalUrl( this, ShouldBeSecure() );
			if( fragmentIdentifier is not null )
				url = url.AddFragmentIdentifier( fragmentIdentifier );

			return url;
		}
		catch( Exception e ) {
			var serializedResource =
				( frameworkResourceSerializer is null ? null : frameworkResourceSerializer( this ) ?? systemSerializationProvider.SerializeResource( this ) ) ??
				appSerializationProviders.Select( i => i.SerializeResource( this ) ).FirstOrDefault( i => i.HasValue ) ??
				throw new UnexpectedValueException( "resource", this );
			throw new Exception(
				"Failed to get a URL for {0}.".FormatWith( serializedResource.name + serializedResource.parameters.PrependDelimiter( " with parameters " ) ),
				e );
		}
	}

	internal sealed IEnumerable<NestedUrl?> GetAllNestedUrls() {
		UrlHandler? urlHandler = this;
		do {
			if( urlHandler is not ResourceParent parent )
				continue;
			foreach( var i in parent.GetLocalNestedUrls() )
				yield return i;
		}
		while( ( urlHandler = urlHandler!.GetParent() ) is not null );
	}

	IEnumerable<NestedUrl?> GetLocalNestedUrls();

	internal bool ShouldBeSecure() {
		// Intermediate installations must be secure because the intermediate user cookie is secure.
		if( ConfigurationStatics.IsIntermediateInstallation && !IsIntermediateInstallationPublicParent )
			return true;

		var connectionSecurity = ConnectionSecurity;
		return connectionSecurity == ConnectionSecurity.MatchingCurrentRequest
			       ? EwfRequest.Current != null && EwfRequest.AppProvider.RequestIsSecure( EwfRequest.Current.AspNetRequest )
			       : connectionSecurity == ConnectionSecurity.SecureIfPossible && EwfConfigurationStatics.AppSupportsSecureConnections;
	}

	/// <summary>
	/// Gets the desired security setting for requests to this parent.
	/// </summary>
	ConnectionSecurity ConnectionSecurity { get; }

	bool AllowsSearchEngineIndexing { get; }
}