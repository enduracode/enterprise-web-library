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

	private static Func<UrlHandlerState>? urlHandlerStateGetter;
	private static Func<ResourceParent, ( string name, string parameters )?>? frameworkResourceSerializer;
	private static SystemProviderReference<SystemResourceSerializationProvider>? systemSerializationProviderRef;

	// Much of the logic in this class exists to prevent *logical* parents (not URL parents) from being created with state from the current URL since that state
	// may not be represented in this item’s URL and therefore behavior could change after a user navigates to this item with a hyperlink. To this end, we force
	// GetUrlParent to be called before CreateParent to ensure that state is available during the former even if called by the latter.
	internal class UrlHandlerCreator<WebItemType> where WebItemType: ResourceParent {
		private readonly Lazy<ResourceParent?> parent;
		private readonly Lazy<UrlHandler?> urlParent;
		private bool creatingUrlParent;

		public readonly Func<WebItemType> ReCreator;
		public readonly UrlHandlerStateOverride? StateOverride;

		public UrlHandlerCreator( WebItemType webItem, Func<WebItemType> reCreator ) {
			parent = new Lazy<ResourceParent?>( () =>
				urlParent!.IsValueCreated
					? UrlHandlerStateOverride.ExecuteWithOverride( true, () => handleParentException( webItem.CreateParent ) )
					: webItem.CreateParent() );

			urlParent = new Lazy<UrlHandler?>( () => {
				UrlHandler? result;

				creatingUrlParent = true;
				try {
					result = StateOverride is null
						         ? handleParentException( webItem.GetUrlParent )
						         : StateOverride.ExecuteWithThis( () => handleParentException( webItem.GetUrlParent ) );
				}
				finally {
					creatingUrlParent = false;
				}

				if( parent.IsValueCreated && !ReferenceEquals( parent.Value, result ) )
					// This means URL state was not disabled during parent creation as it should have been.
					throw new Exception(
						"You cannot call Parent on a resource or entity setup from getUrlParent on the same object unless you return it as the result." );

				return result;
			} );

			ReCreator = () => StateOverride is null
				                  ? reCreator()
				                  : UrlHandlerStateOverride.ExecuteWithOverride(
					                  false,
					                  () => {
						                  UrlHandlerStateOverride.Current!.Set( webItem );
						                  return reCreator();
					                  } );

			StateOverride = UrlHandlerStateOverride.Current;

			return;
			static T handleParentException<T>( Func<T> method ) {
				try {
					return method();
				}
				catch( Exception e ) {
					throw new ResourceAncestorException( e );
				}
			}
		}

		public ResourceParent? Parent {
			get {
				if( !creatingUrlParent )
					_ = urlParent.Value;

				return parent.Value;
			}
		}

		public ResourceParent? CreatedParent => parent.IsValueCreated ? parent.Value : null;

		public UrlHandler? UrlParent => urlParent.Value;
	}

	protected internal class UrlHandlerState {
		private static IEnumerable<ResourceParent> getNewParameterValueWebItems( ResourceParent? webItem ) {
			do
				yield return webItem!;
			while( ( webItem = webItem!.CreatedParent ) is not null );
		}

		public readonly IReadOnlyCollection<BasicUrlHandler> Handlers;
		public readonly IEnumerable<ResourceParent> NewParameterValueWebItems;

		public UrlHandlerState( IReadOnlyCollection<BasicUrlHandler> handlers, SpecifiedValue<ResourceParent>? newParameterValueWebItem ) {
			Handlers = handlers;
			NewParameterValueWebItems = newParameterValueWebItem is null ? [ ] : getNewParameterValueWebItems( newParameterValueWebItem.Value );
		}
	}

	internal static void Init(
		Func<UrlHandlerState> urlHandlerStateGetter, Func<ResourceParent, ( string, string )?> frameworkResourceSerializer,
		SystemProviderReference<SystemResourceSerializationProvider> systemSerializationProvider,
		SystemProviderReference<AppResourceSerializationProvider> appSerializationProvider ) {
		ResourceParent.urlHandlerStateGetter = urlHandlerStateGetter;
		ResourceParent.frameworkResourceSerializer = frameworkResourceSerializer;
		systemSerializationProviderRef = systemSerializationProvider;
		appSerializationProviderRefs.Add( appSerializationProvider );
	}

	internal static void AddApplication( SystemProviderReference<AppResourceSerializationProvider> provider ) {
		appSerializationProviderRefs.Add( provider );
	}

	private static SystemResourceSerializationProvider systemSerializationProvider => systemSerializationProviderRef!.GetProvider()!;
	private static IEnumerable<AppResourceSerializationProvider> appSerializationProviders => appSerializationProviderRefs.Select( i => i.GetProvider()! );

	internal sealed UrlHandlerState GetUrlHandlerState() => urlHandlerStateGetter!();

	/// <summary>
	/// Gets the parent of this parent, or null if there isn’t one.
	/// </summary>
	ResourceParent? Parent { get; }

	internal ResourceParent? CreatedParent { get; }

	internal ResourceParent? CreateParent();

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

	internal IEnumerable<NestedUrl?> GetLocalNestedUrls();

	internal UrlHandler? GetUrlParent();

	internal sealed bool ShouldBeSecure() {
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