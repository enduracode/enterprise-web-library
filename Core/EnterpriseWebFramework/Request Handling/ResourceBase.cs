using System.ComponentModel;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.DataAccess;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using StackExchange.Profiling;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A base set of functionality that can be used to discover information about a resource before actually requesting it.
/// </summary>
[ PublicAPI ]
public abstract class ResourceBase: ResourceInfo, ResourceParent {
	private static Func<ResourceBase, ( string name, string parameters )?> frameworkResourceSerializer;
	private static SystemProviderReference<SystemResourceSerializationProvider> systemSerializationProviderRef;
	private static SystemProviderReference<AppResourceSerializationProvider> appSerializationProviderRef;
	private static Action<bool, ResourceBase> urlHandlerStateUpdater;
	private static Func<ResourceBase> currentResourceGetter;

	internal static void WriteRedirectResponse( HttpContext context, string url, bool permanent ) {
		if( context.Request.Method == "GET" || context.Request.Method == "HEAD" )
			EwfSafeResponseWriter.AddCacheControlHeader(
				context.Response,
				EwfRequest.AppBaseUrlProvider.RequestIsSecure( context.Request ),
				false,
				permanent && !ConfigurationStatics.IsDevelopmentInstallation ? null : false );

		EwfResponse.Create(
				ContentTypes.PlainText,
				new EwfResponseBodyCreator( writer => writer.Write( "{0} Redirect: {1}".FormatWith( permanent ? "Permanent" : "Temporary", url ) ) ),
				statusCodeGetter: () => permanent && !ConfigurationStatics.IsDevelopmentInstallation ? 308 : 307,
				additionalHeaderFieldGetter: () => ( "Location", url ).ToCollection() )
			.WriteToAspNetResponse( context.Response, omitBody: context.Request.Method == "HEAD" );
	}

	internal static void Init(
		Func<ResourceBase, ( string, string )?> frameworkResourceSerializer,
		SystemProviderReference<SystemResourceSerializationProvider> systemSerializationProvider,
		SystemProviderReference<AppResourceSerializationProvider> appSerializationProvider, Action<bool, ResourceBase> urlHandlerStateUpdater,
		Func<ResourceBase> currentResourceGetter ) {
		ResourceBase.frameworkResourceSerializer = frameworkResourceSerializer;
		systemSerializationProviderRef = systemSerializationProvider;
		appSerializationProviderRef = appSerializationProvider;
		ResourceBase.urlHandlerStateUpdater = urlHandlerStateUpdater;
		ResourceBase.currentResourceGetter = currentResourceGetter;
	}

	private static SystemResourceSerializationProvider systemSerializationProvider => systemSerializationProviderRef.GetProvider();
	private static AppResourceSerializationProvider appSerializationProvider => appSerializationProviderRef.GetProvider();

	/// <summary>
	/// Gets the currently executing resource, or null if the URL has not yet been resolved.
	/// </summary>
	internal static ResourceBase Current => currentResourceGetter();

	private string uriFragmentIdentifierField = "";
	private readonly Lazy<ResourceParent> parent;
	private readonly Lazy<string> name;
	private readonly Lazy<AlternativeResourceMode> alternativeMode;
	private readonly Lazy<UrlHandler> urlParent;

	/// <summary>
	/// Creates a resource object.
	/// </summary>
	protected ResourceBase() {
		parent = new Lazy<ResourceParent>( createParent );
		name = new Lazy<string>( getResourceName );
		alternativeMode = new Lazy<AlternativeResourceMode>( createAlternativeMode );
		urlParent = new Lazy<UrlHandler>( getUrlParent );
	}

	/// <summary>
	/// Throws an exception if the parameter values or any non URL elements of the current request make the resource invalid.
	/// </summary>
	protected virtual void init() {}

	/// <summary>
	/// Executes the specified fragment identifier validator, which takes the resource's fragment identifier and should return a non empty string, i.e. an error
	/// message, if the identifier is not valid. This method should only be called from init or from custom constructors. The validator should not expose the
	/// identifier to other parts of the class.
	/// </summary>
	protected void executeFragmentIdentifierValidatorIfNecessary( Func<string, string> validator ) {
		// If this info object is being created for the current request, skip validation since the fragment identifier will not be available. User agents are not
		// supposed to include it in requests. See section 3.5 of http://www.ietf.org/rfc/rfc3986.txt.
		if( EwfRequest.Current != null && Current == null )
			return;

		var message = validator( uriFragmentIdentifier );
		if( message.Length > 0 )
			throw new ApplicationException( message );
	}

	/// <summary>
	/// EWL Core and auto-generated code use only.
	/// </summary>
	[ EditorBrowsable( EditorBrowsableState.Never ) ]
	protected string uriFragmentIdentifier {
		get => uriFragmentIdentifierField;
		set {
			// We think the validation will be better if we tell the URI class the scheme via an absolute URI instead of just giving it the fragment identifier
			// by itself and using UriKind.Relative. The scheme can affect the validation that is performed.
			if( new[] { "http", "https" }.Any( i => !Uri.IsWellFormedUriString( i + "://dummy.com/dummy" + value.PrependDelimiter( "#" ), UriKind.Absolute ) ) )
				throw new ApplicationException( "invalid URI fragment identifier" );

			uriFragmentIdentifierField = value;
		}
	}

	/// <summary>
	/// Gets the entity setup for this resource, if one exists.
	/// </summary>
	public abstract EntitySetupBase? EsAsBaseType { get; }

	/// <summary>
	/// Gets the parent of this resource, or null if there isn’t one.
	/// </summary>
	public ResourceParent Parent => parent.Value;

	/// <summary>
	/// Gets the parent resource of this resource. Throws an exception if there is no parent or the parent is an entity setup.
	/// </summary>
	public ResourceBase ParentResource =>
		Parent as ResourceBase ?? throw new Exception( "The resource either doesn’t have a parent or the parent is an entity setup." );

	/// <summary>
	/// Gets whether an entity setup exists and is also the parent.
	/// </summary>
	internal bool EntitySetupIsParent => EsAsBaseType is not null && ReferenceEquals( Parent, EsAsBaseType );

	/// <summary>
	/// Creates the parent of this resource. Returns null if there is no parent.
	/// </summary>
	protected virtual ResourceParent createParent() => EsAsBaseType;

	/// <summary>
	/// Gets the ancestors of this resource, from the parent all the way up to the root.
	/// </summary>
	public IEnumerable<ResourceParent> Ancestors {
		get {
			var ancestors = new List<ResourceParent>();
			ResourceParent p = this;
			while( ( p = p.Parent ) != null )
				ancestors.Add( p );
			return ancestors;
		}
	}

	string ResourceParent.Name => ResourceName;

	/// <summary>
	/// Gets the name of this resource.
	/// </summary>
	public string ResourceName => name.Value.Length == 0 && Parent is not null ? throw new Exception( "Every non-root resource must have a name." ) : name.Value;

	/// <summary>
	/// Returns the name of this resource. Never return null. If this is a root resource, return the empty string to make it a home.
	/// </summary>
	protected virtual string getResourceName() => GetType().Name.CamelToEnglish();

	/// <summary>
	/// Gets the name of this resource, including the entity setup name if an entity setup exists.
	/// </summary>
	public string ResourceFullName {
		get {
			var fullName = ResourceName;
			if( EsAsBaseType is not null )
				foreach( var i in Ancestors ) {
					var entitySetup = i as EntitySetupBase ?? ( (ResourceBase)i ).EsAsBaseType;
					if( !ReferenceEquals( entitySetup, EsAsBaseType ) )
						break;
					fullName = StringTools.ConcatenateWithDelimiter( " > ", i.Name.Length == 0 && i.Parent is null ? "Home" : i.Name, fullName );
				}
			return fullName;
		}
	}

	/// <summary>
	/// Gets whether the authenticated user is authorized to access this resource.
	/// </summary>
	public sealed override bool UserCanAccess {
		get {
			if( ConfigurationStatics.IsIntermediateInstallation ) {
				if( IsIntermediateInstallationPublicResource )
					return true;
				if( EwfRequest.Current == null || !RequestDispatchingStatics.RequestState.IntermediateUserExists )
					return false;
			}

			// It’s important to do the entity setup and parent checks first so the resource doesn’t have to repeat any of it. For example, if the entity setup
			// verifies that the authenticated user is not null, the resource should be able to assume this when doing its own checks.
			return ( EsAsBaseType is null || EntitySetupIsParent || EsAsBaseType.UserCanAccess ) && ( Parent is null || Parent.UserCanAccess ) && userCanAccess;
		}
	}

	/// <summary>
	/// Gets whether the resource is public in intermediate installations, regardless of other authorization logic that may exist.
	/// </summary>
	protected internal virtual bool IsIntermediateInstallationPublicResource => false;

	/// <summary>
	/// Gets whether the authenticated user passes resource-level authorization checks.
	/// </summary>
	protected virtual bool userCanAccess => true;

	/// <summary>
	/// Gets the log-in page to use for this resource, or null for default behavior.
	/// </summary>
	public virtual ResourceBase LogInPage => Parent?.LogInPage;

	/// <summary>
	/// Gets the alternative mode for this resource or null if it is in normal mode. Do not call this from the createAlternativeMode method of an ancestor;
	/// doing so will result in a stack overflow.
	/// </summary>
	public sealed override AlternativeResourceMode AlternativeMode =>
		// It’s important to do the entity setup and parent disabled checks first so the resource doesn’t have to repeat any of them in its disabled check.
		EsAsBaseType is not null && !EntitySetupIsParent && EsAsBaseType.AlternativeMode is DisabledResourceMode ? EsAsBaseType.AlternativeMode :
		Parent?.AlternativeMode is DisabledResourceMode ? Parent.AlternativeMode : AlternativeModeDirect;

	/// <summary>
	/// Gets the alternative mode for this resource without using ancestor logic. Useful when called from the createAlternativeMode method of an ancestor, e.g.
	/// when implementing a parent that should have new content when one or more children have new content. When calling this property take care to meet any
	/// preconditions that would normally be handled by ancestor logic.
	/// </summary>
	public AlternativeResourceMode AlternativeModeDirect => alternativeMode.Value;

	/// <summary>
	/// Creates the alternative mode for this resource or returns null if it is in normal mode.
	/// </summary>
	protected virtual AlternativeResourceMode createAlternativeMode() => null;

	internal sealed override string GetUrl( bool ensureUserCanAccessResource, bool ensureResourceNotDisabled ) {
		try {
			if( ensureUserCanAccessResource && !UserCanAccess )
				throw new ApplicationException( "The authenticated user cannot access the resource." );
			if( ensureResourceNotDisabled && AlternativeMode is DisabledResourceMode )
				throw new ApplicationException( "The resource is disabled." );

			string getCanonicalUrl() => UrlHandlingStatics.GetCanonicalUrl( this, ShouldBeSecureGivenCurrentRequest );
			return ( EwfRequest.Current != null ? RequestDispatchingStatics.RequestState.ExecuteWithUserDisabled( getCanonicalUrl ) : getCanonicalUrl() ) +
			       uriFragmentIdentifier.PrependDelimiter( "#" );
		}
		catch( Exception e ) {
			var serializedResource = frameworkResourceSerializer( this ) ?? systemSerializationProvider.SerializeResource( this ) ??
			                         appSerializationProvider.SerializeResource( this ) ?? throw new UnexpectedValueException( "resource", this );
			throw new Exception(
				"Failed to get a URL for {0}.".FormatWith( serializedResource.name + serializedResource.parameters.PrependDelimiter( " with parameters " ) ),
				e );
		}
	}

	UrlHandler UrlHandler.GetParent() => urlParent.Value;

	/// <summary>
	/// Returns the resource or entity setup that will determine this resource’s canonical URL. One reason to override is if <see cref="createParent"/> depends on
	/// the authenticated user since the URL must not have this dependency.
	/// </summary>
	protected virtual UrlHandler getUrlParent() => Parent;

	UrlEncoder BasicUrlHandler.GetEncoder() => getUrlEncoder();

	/// <summary>
	/// Returns a URL encoder for this resource. Framework use only.
	/// </summary>
	protected abstract UrlEncoder getUrlEncoder();

	internal bool ShouldBeSecureGivenCurrentRequest {
		get {
			// Intermediate installations must be secure because the intermediate user cookie is secure.
			if( ConfigurationStatics.IsIntermediateInstallation && !IsIntermediateInstallationPublicResource )
				return true;

			var connectionSecurity = ConnectionSecurity;
			return connectionSecurity == ConnectionSecurity.MatchingCurrentRequest
				       ? EwfRequest.Current != null && EwfRequest.AppBaseUrlProvider.RequestIsSecure( EwfRequest.Current.AspNetRequest )
				       : connectionSecurity == ConnectionSecurity.SecureIfPossible && EwfConfigurationStatics.AppSupportsSecureConnections;
		}
	}

	/// <summary>
	/// Gets the desired security setting for requests to the resource.
	/// </summary>
	public virtual ConnectionSecurity ConnectionSecurity => Parent?.ConnectionSecurity ?? ConnectionSecurity.SecureIfPossible;

	( UrlHandler parent, UrlHandler child ) UrlHandler.GetCanonicalHandlerPair( UrlHandler child ) => ( this, child );

	IEnumerable<UrlHandler> UrlHandler.GetRequestHandlingDescendants() => Enumerable.Empty<UrlHandler>();

	IEnumerable<UrlPattern> UrlHandler.GetChildPatterns() => getChildUrlPatterns();

	/// <summary>
	/// Returns this resource’s child URL patterns. Must not depend on the authenticated user.
	/// </summary>
	protected virtual IEnumerable<UrlPattern> getChildUrlPatterns() => Enumerable.Empty<UrlPattern>();

	void BasicUrlHandler.HandleRequest( HttpContext context ) => HandleRequest( context, false );

	internal void HandleRequest( HttpContext context, bool requestTransferred ) {
		var canonicalUrl = GetUrl( false, false );
		if( requestTransferred ) {
			if( ShouldBeSecureGivenCurrentRequest != EwfRequest.AppBaseUrlProvider.RequestIsSecure( context.Request ) )
				throw new ApplicationException( "{0} has a connection security setting that is incompatible with the current request.".FormatWith( canonicalUrl ) );
		}
		else {
			if( disablesUrlNormalization ) {
				if( ShouldBeSecureGivenCurrentRequest != EwfRequest.AppBaseUrlProvider.RequestIsSecure( context.Request ) )
					throw new ResourceNotAvailableException( "The resource has a connection security setting that is incompatible with the current request.", null );
			}
			else {
				if( canonicalUrl != AppRequestState.Instance.Url ) {
					if( !ShouldBeSecureGivenCurrentRequest && EwfRequest.AppBaseUrlProvider.RequestIsSecure( context.Request ) )
						context.Response.Headers.StrictTransportSecurity = "max-age=0";
					WriteRedirectResponse( context, canonicalUrl, true );
					return;
				}
			}
		}

		bool userAuthorized;
		using( MiniProfiler.Current.Step( "EWF - Check resource authorization" ) )
			userAuthorized = UserCanAccess;
		if( !userAuthorized )
			throw new AccessDeniedException(
				ConfigurationStatics.IsIntermediateInstallation && !IsIntermediateInstallationPublicResource && !AppRequestState.Instance.IntermediateUserExists,
				LogInPage );

		DisabledResourceMode disabledMode;
		using( MiniProfiler.Current.Step( "EWF - Check alternative resource mode" ) )
			disabledMode = AlternativeMode as DisabledResourceMode;
		if( disabledMode != null )
			throw new PageDisabledException( disabledMode.Message );

		urlHandlerStateUpdater( requestTransferred, this );

		var redirect = getRedirect();
		if( redirect != null ) {
			if( requestTransferred )
				throw new ApplicationException( "A redirect is not valid when the request has been transferred." );
			WriteRedirectResponse( context, redirect.Resource.GetUrl(), redirect.IsPermanent );
			return;
		}

		if( context.Request.Method == "GET" || context.Request.Method == "HEAD" || requestTransferred ) {
			var requestHandler = getOrHead();
			if( requestHandler != null )
				requestHandler.WriteResponse( context, requestTransferred );
			else {
				if( requestTransferred )
					throw new ApplicationException( "getOrHead must be implemented when the request has been transferred." );
				EwfResponse.Create(
						"",
						new EwfResponseBodyCreator( () => "" ),
						statusCodeGetter: () => 405,
						additionalHeaderFieldGetter: () => ( "Allow", "" ).ToCollection() )
					.WriteToAspNetResponse( context.Response );
			}
			return;
		}

		var response = context.Request.Method switch
			{
				"PUT" => executeUnsafeRequestMethod( put ),
				"PATCH" => executeUnsafeRequestMethod( patch ),
				"DELETE" => executeUnsafeRequestMethod( delete ),
				"POST" => executeUnsafeRequestMethod( post ),
				_ => EwfResponse.Create( "", new EwfResponseBodyCreator( () => "" ), statusCodeGetter: () => 501 )
			} ?? EwfResponse.Create(
			"",
			new EwfResponseBodyCreator( () => "" ),
			statusCodeGetter: () => 405,
			additionalHeaderFieldGetter: () => ( "Allow", "" ).ToCollection() );
		response.WriteToAspNetResponse( context.Response );
	}

	/// <summary>
	/// Gets whether URL normalization is disabled when the resource is requested.
	/// </summary>
	protected virtual bool disablesUrlNormalization => false;

	/// <summary>
	/// Returns the redirect for the resource, if it is located outside of the application.
	/// </summary>
	protected virtual ExternalRedirect getRedirect() => null;

	/// <summary>
	/// Returns the handler for a GET or HEAD request.
	/// </summary>
	protected virtual EwfSafeRequestHandler getOrHead() => null;

	private EwfResponse executeUnsafeRequestMethod( Func<EwfResponse> method ) {
		if( managesDataAccessCacheInUnsafeRequestMethods )
			return method();

		DataAccessState.Current.DisableCache();
		try {
			return method();
		}
		finally {
			DataAccessState.Current.ResetCache();
		}
	}

	protected virtual bool managesDataAccessCacheInUnsafeRequestMethods => false;

	protected virtual EwfResponse put() => null;
	protected virtual EwfResponse patch() => null;
	protected virtual EwfResponse delete() => null;
	protected virtual EwfResponse post() => null;

	public virtual bool AllowsSearchEngineIndexing => Parent?.AllowsSearchEngineIndexing ?? true;

	/// <summary>
	/// Returns true if this resource is identical to the current resource.
	/// </summary>
	public virtual bool MatchesCurrent() => Equals( Current );

	/// <summary>
	/// Framework use only.
	/// </summary>
	protected internal abstract ResourceBase ReCreate();

	public sealed override bool Equals( object? obj ) => Equals( obj as BasicUrlHandler );
	public abstract bool Equals( BasicUrlHandler? other );
	public abstract override int GetHashCode();
}