using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An object that allows several pages and resources to share query parameters, authorization logic, data, etc.
	/// </summary>
	public abstract class EntitySetupBase: UrlHandler {
		private readonly Lazy<ResourceBase> parentResource;
		private readonly Lazy<AlternativeResourceMode> alternativeMode;
		private readonly Lazy<IReadOnlyCollection<ResourceGroup>> listedResources;
		private readonly Lazy<UrlHandler> urlParent;

		/// <summary>
		/// Creates an entity setup object.
		/// </summary>
		protected EntitySetupBase() {
			parentResource = new Lazy<ResourceBase>( createParentResource );
			alternativeMode = new Lazy<AlternativeResourceMode>( createAlternativeMode );
			listedResources = new Lazy<IReadOnlyCollection<ResourceGroup>>( () => createListedResources().Materialize() );
			urlParent = new Lazy<UrlHandler>( getUrlParent );
		}

		/// <summary>
		/// Throws an exception if the parameter values or any non URL elements of the current request make the entity setup invalid.
		/// </summary>
		protected virtual void init() {}

		/// <summary>
		/// Gets the parent resource, or null if there is no parent.
		/// </summary>
		public ResourceBase ParentResource => parentResource.Value;

		/// <summary>
		/// Creates the parent resource. Returns null if there is no parent.
		/// </summary>
		protected abstract ResourceBase createParentResource();

		/// <summary>
		/// Returns the name of the entity setup.
		/// </summary>
		public abstract string EntitySetupName { get; }

		/// <summary>
		/// Returns true if the authenticated user passes entity setup authorization checks.
		/// </summary>
		protected internal virtual bool UserCanAccessEntitySetup => true;

		/// <summary>
		/// Gets the log-in page to use for resources that are part of this entity setup, or null for default behavior.
		/// </summary>
		protected internal virtual ResourceBase LogInPage => ParentResource?.LogInPage;

		/// <summary>
		/// Gets the alternative mode for this entity setup or null if it is in normal mode. Do not call this from the createAlternativeMode method of an ancestor;
		/// doing so will result in a stack overflow.
		/// </summary>
		public AlternativeResourceMode AlternativeMode {
			get {
				// It's important to do the parent disabled check first so the entity setup doesn't have to repeat any of it in its disabled check.
				if( ParentResource != null && ParentResource.AlternativeMode is DisabledResourceMode )
					return ParentResource.AlternativeMode;
				return AlternativeModeDirect;
			}
		}

		/// <summary>
		/// Gets the alternative mode for this entity setup without using ancestor logic. Useful when called from the createAlternativeMode method of an ancestor,
		/// e.g. when implementing a parent that should have new content when one or more children have new content. When calling this property take care to meet
		/// any preconditions that would normally be handled by ancestor logic.
		/// </summary>
		public AlternativeResourceMode AlternativeModeDirect => alternativeMode.Value;

		/// <summary>
		/// Creates the alternative mode for this entity setup or returns null if it is in normal mode.
		/// </summary>
		protected virtual AlternativeResourceMode createAlternativeMode() {
			return null;
		}

		/// <summary>
		/// Initializes the parameters modification object for this entity setup.
		/// </summary>
		protected internal abstract void InitParametersModification();

		/// <summary>
		/// Gets a list of groups containing this entity setup’s listed resources.
		/// </summary>
		public IReadOnlyCollection<ResourceGroup> ListedResources => listedResources.Value;

		/// <summary>
		/// Creates a list of groups containing this entity setup’s listed resources.
		/// </summary>
		protected abstract IEnumerable<ResourceGroup> createListedResources();

		UrlHandler UrlHandler.GetParent() => urlParent.Value;

		/// <summary>
		/// Returns the resource or entity setup that will determine this entity setup’s canonical URL. One reason to override is if
		/// <see cref="createParentResource"/> depends on the authenticated user since the URL must not have this dependency.
		/// </summary>
		protected virtual UrlHandler getUrlParent() => ParentResource;

		UrlEncoder BasicUrlHandler.GetEncoder() => getUrlEncoder();

		/// <summary>
		/// Returns a URL encoder for this entity setup. Framework use only.
		/// </summary>
		protected abstract UrlEncoder getUrlEncoder();

		/// <summary>
		/// Gets the desired security setting for requests to resources that are part of this entity setup.
		/// </summary>
		protected internal virtual ConnectionSecurity ConnectionSecurity => ParentResource?.ConnectionSecurity ?? ConnectionSecurity.SecureIfPossible;

		( UrlHandler parent, UrlHandler child ) UrlHandler.GetCanonicalHandlerPair( UrlHandler child ) {
			var requestHandler = AppRequestState.ExecuteWithUrlHandlerStateDisabled( getRequestHandler );
			return EwlStatics.AreEqual( child, requestHandler ) && canRepresentRequestHandler()
				       ? ( (UrlHandler)this ).GetParent()?.GetCanonicalHandlerPair( this ) ?? ( null, this )
				       : ( this, child );
		}

		IEnumerable<UrlHandler> UrlHandler.GetRequestHandlingDescendants() {
			UrlHandler requestHandler;
			try {
				requestHandler = getRequestHandler();
			}
			catch( Exception e ) {
				if( e is UserDisabledException )
					throw;
				throw new UnresolvableUrlException( "Failed to get the request handler.", e );
			}

			return requestHandler != null ? requestHandler.ToCollection().Concat( requestHandler.GetRequestHandlingDescendants() ) : Enumerable.Empty<UrlHandler>();
		}

		/// <summary>
		/// Returns the resource or entity setup that will handle an HTTP request to this entity setup. Normally this should be the first of the listed resources.
		/// We do not recommend returning null; doing so will make the entity setup URL unusable. Must not depend on the authenticated user.
		/// </summary>
		protected abstract UrlHandler getRequestHandler();

		/// <summary>
		/// Returns whether this entity setup’s canonical URL will be the canonical URL for the request handler, if the request handler specifies this entity setup
		/// as its parent. Must not depend on the authenticated user.
		/// </summary>
		protected virtual bool canRepresentRequestHandler() => false;

		IEnumerable<UrlPattern> UrlHandler.GetChildPatterns() => getChildUrlPatterns();

		/// <summary>
		/// Returns this entity setup’s child URL patterns. Must not depend on the authenticated user.
		/// </summary>
		protected virtual IEnumerable<UrlPattern> getChildUrlPatterns() => Enumerable.Empty<UrlPattern>();

		void BasicUrlHandler.HandleRequest( HttpContext context ) {
			throw new ResourceNotAvailableException( "An entity setup cannot handle a request.", null );
		}

		protected internal virtual bool AllowsSearchEngineIndexing => ParentResource?.AllowsSearchEngineIndexing ?? true;

		public sealed override bool Equals( object obj ) => Equals( obj as BasicUrlHandler );
		public abstract bool Equals( BasicUrlHandler other );
		public abstract override int GetHashCode();
	}
}