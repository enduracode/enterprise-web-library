using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using EnterpriseWebLibrary.Configuration;
using Humanizer;
using StackExchange.Profiling;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A base set of functionality that can be used to discover information about a resource before actually requesting it.
	/// </summary>
	public abstract class ResourceBase: ResourceInfo, UrlHandler {
		internal const string ResourcePathSeparator = " > ";
		internal const string EntityResourceSeparator = " / ";

		internal static string CombineResourcePathStrings( string separator, string one, string two, params string[] pathStrings ) {
			var pathString = StringTools.ConcatenateWithDelimiter( separator, one, two );
			foreach( var s in pathStrings )
				pathString = StringTools.ConcatenateWithDelimiter( separator, pathString, s );
			return pathString;
		}

		private string uriFragmentIdentifierField = "";
		private readonly Lazy<ResourceBase> parentResource;
		private readonly Lazy<AlternativeResourceMode> alternativeMode;

		/// <summary>
		/// Creates a resource info object.
		/// </summary>
		protected ResourceBase() {
			parentResource = new Lazy<ResourceBase>( createParentResource );
			alternativeMode = new Lazy<AlternativeResourceMode>( createAlternativeMode );
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
			if( EwfPage.Instance != null && EwfPage.Instance.InfoAsBaseType == null )
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
			get { return uriFragmentIdentifierField; }
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
		public abstract EntitySetupBase EsAsBaseType { get; }

		/// <summary>
		/// Gets the parent resource, or null if there is no parent.
		/// </summary>
		public ResourceBase ParentResource => parentResource.Value;

		/// <summary>
		/// Creates the parent resource. Returns null if there is no parent.
		/// </summary>
		protected virtual ResourceBase createParentResource() => null;

		/// <summary>
		/// Gets the resource path from the root all the way down to the current resource.
		/// </summary>
		public List<ResourceBase> ResourcePath {
			get {
				// NOTE: If we used recursion this would be much simpler.
				var path = new List<ResourceBase>();
				var resource = this;
				do
					path.Add( resource );
				while( ( resource = resource.ParentResource ?? resource.EsAsBaseType?.ParentResource ) != null );
				path.Reverse();
				return path;
			}
		}

		/// <summary>
		/// Returns the name of the resource.
		/// </summary>
		public virtual string ResourceName => GetType().DeclaringType.Name.CamelToEnglish();

		/// <summary>
		/// Returns the name of the resource, including the entity setup name if an entity setup exists.
		/// </summary>
		public string ResourceFullName =>
			CombineResourcePathStrings( EntityResourceSeparator, EsAsBaseType != null && ParentResource == null ? EsAsBaseType.EntitySetupName : "", ResourceName );

		/// <summary>
		/// Returns the string representing the parent resource's path within the entity.
		/// </summary>
		internal string ParentResourceEntityPathString {
			get {
				if( EsAsBaseType == null )
					return "";
				var pathString = "";
				for( var i = 0; i < ResourcePath.Count - 1; i += 1 ) {
					// Traverse the path backwards, excluding this resource.
					var resource = ResourcePath[ ResourcePath.Count - 2 - i ];

					// NOTE: There should be a third part of this condition that tests if all the parameters in the entity setups are the same.
					if( resource.EsAsBaseType == null || !resource.EsAsBaseType.GetType().Equals( EsAsBaseType.GetType() ) )
						break;

					pathString = CombineResourcePathStrings( ResourcePathSeparator, resource.ResourceFullName, pathString );
				}
				return pathString;
			}
		}

		/// <summary>
		/// Returns true if the authenticated user is authorized to access the resource.
		/// </summary>
		public sealed override bool UserCanAccessResource {
			get {
				if( ConfigurationStatics.IsIntermediateInstallation ) {
					if( IsIntermediateInstallationPublicResource )
						return true;
					if( !AppRequestState.Instance.IntermediateUserExists )
						return false;
				}

				// It's important to do the entity setup check first so the resource doesn't have to repeat any of it. For example, if the entity setup verifies that
				// the authenticated user is not null, the resource should be able to assume this when doing its own checks.
				return ( EsAsBaseType != null
					         ? ( EsAsBaseType.ParentResource != null ? EsAsBaseType.ParentResource.UserCanAccessResource : true ) && EsAsBaseType.UserCanAccessEntitySetup
					         : true ) && ( ParentResource != null ? ParentResource.UserCanAccessResource : true ) && userCanAccessResource;
			}
		}

		/// <summary>
		/// Gets whether the resource is public in intermediate installations.
		/// </summary>
		protected internal virtual bool IsIntermediateInstallationPublicResource => false;

		/// <summary>
		/// Returns true if the authenticated user passes resource-level authorization checks.
		/// </summary>
		protected virtual bool userCanAccessResource => true;

		/// <summary>
		/// Gets the log in page to use for this resource if the system supports forms authentication.
		/// </summary>
		protected internal virtual PageInfo LogInPage {
			get {
				if( ParentResource != null )
					return ParentResource.LogInPage;
				if( EsAsBaseType != null )
					return EsAsBaseType.LogInPage;
				return null;
			}
		}

		/// <summary>
		/// Gets the alternative mode for this resource or null if it is in normal mode. Do not call this from the createAlternativeMode method of an ancestor;
		/// doing so will result in a stack overflow.
		/// </summary>
		public sealed override AlternativeResourceMode AlternativeMode {
			get {
				// It's important to do the entity setup and parent disabled checks first so the resource doesn't have to repeat any of them in its disabled check.
				if( EsAsBaseType != null && EsAsBaseType.AlternativeMode is DisabledResourceMode )
					return EsAsBaseType.AlternativeMode;
				if( ParentResource != null && ParentResource.AlternativeMode is DisabledResourceMode )
					return ParentResource.AlternativeMode;
				return AlternativeModeDirect;
			}
		}

		/// <summary>
		/// Gets the alternative mode for this resource without using ancestor logic. Useful when called from the createAlternativeMode method of an ancestor, e.g.
		/// when implementing a parent that should have new content when one or more children have new content. When calling this property take care to meet any
		/// preconditions that would normally be handled by ancestor logic.
		/// </summary>
		public AlternativeResourceMode AlternativeModeDirect => alternativeMode.Value;

		/// <summary>
		/// Creates the alternative mode for this resource or returns null if it is in normal mode.
		/// </summary>
		protected virtual AlternativeResourceMode createAlternativeMode() {
			return null;
		}

		internal sealed override string GetUrl( bool ensureUserCanAccessResource, bool ensureResourceNotDisabled, bool makeAbsolute ) {
			var url = UrlHandlingStatics.GetCanonicalUrl( this, ShouldBeSecureGivenCurrentRequest ) + uriFragmentIdentifier.PrependDelimiter( "#" );
			if( ensureUserCanAccessResource && !UserCanAccessResource )
				throw new ApplicationException( "GetUrl was called for a resource that the authenticated user cannot access. The URL would have been " + url + "." );
			if( ensureResourceNotDisabled && AlternativeMode is DisabledResourceMode )
				throw new ApplicationException( "GetUrl was called for a resource that is disabled. The URL would have been " + url + "." );
			return url;
		}

		UrlHandler UrlHandler.GetParent() => getUrlParent();

		/// <summary>
		/// Returns the resource or entity setup that will determine this resource’s canonical URL. One reason to override is if <see cref="createParentResource"/>
		/// depends on the authenticated user since the URL must not have this dependency.
		/// </summary>
		protected virtual UrlHandler getUrlParent() => (UrlHandler)ParentResource ?? EsAsBaseType;

		UrlEncoder BasicUrlHandler.GetEncoder() => getUrlEncoder();

		/// <summary>
		/// Returns a URL encoder for this resource. Framework use only.
		/// </summary>
		protected abstract UrlEncoder getUrlEncoder();

		internal bool ShouldBeSecureGivenCurrentRequest => ConnectionSecurity.ShouldBeSecureGivenCurrentRequest( IsIntermediateInstallationPublicResource );

		/// <summary>
		/// Gets the desired security setting for requests to the resource.
		/// </summary>
		protected internal virtual ConnectionSecurity ConnectionSecurity {
			get {
				if( ParentResource != null )
					return ParentResource.ConnectionSecurity;
				if( EsAsBaseType != null )
					return EsAsBaseType.ConnectionSecurity;
				return ConnectionSecurity.SecureIfPossible;
			}
		}

		( UrlHandler parent, UrlHandler child ) UrlHandler.GetCanonicalHandlerPair( UrlHandler child ) => ( this, child );

		IEnumerable<UrlPattern> UrlHandler.GetChildPatterns() => getChildUrlPatterns();

		/// <summary>
		/// Returns this resource’s child URL patterns. Must not depend on the authenticated user.
		/// </summary>
		protected virtual IEnumerable<UrlPattern> getChildUrlPatterns() => Enumerable.Empty<UrlPattern>();

		void BasicUrlHandler.HandleRequest( HttpContext context ) {
			AppRequestState.Instance.UserDisabledByResource = true;
			try {
				var canonicalUrl = GetUrl( false, false, true );
				if( canonicalUrl != AppRequestState.Instance.Url ) {
					if( !ShouldBeSecureGivenCurrentRequest && EwfApp.Instance.RequestIsSecure( context.Request ) )
						context.Response.AppendHeader( "Strict-Transport-Security", "max-age=0" );
					writeRedirectResponse( context, canonicalUrl, true );
					return;
				}
			}
			finally {
				AppRequestState.Instance.UserDisabledByResource = false;
			}

			bool userAuthorized;
			using( MiniProfiler.Current.Step( "EWF - Check resource authorization" ) )
				userAuthorized = UserCanAccessResource;
			if( !userAuthorized )
				throw new AccessDeniedException(
					ConfigurationStatics.IsIntermediateInstallation && !IsIntermediateInstallationPublicResource && !AppRequestState.Instance.IntermediateUserExists,
					LogInPage );

			DisabledResourceMode disabledMode;
			using( MiniProfiler.Current.Step( "EWF - Check alternative resource mode" ) )
				disabledMode = AlternativeMode as DisabledResourceMode;
			if( disabledMode != null )
				throw new PageDisabledException( disabledMode.Message );

			var redirect = getRedirect();
			if( redirect != null ) {
				writeRedirectResponse( context, redirect.Resource.GetUrl(), redirect.IsPermanent );
				return;
			}

			if( context.Request.HttpMethod == "GET" || context.Request.HttpMethod == "HEAD" ) {
				var requestHandler = getOrHead();
				if( requestHandler != null )
					requestHandler.WriteResponse();
				else {
					context.Response.StatusCode = 405;
					EwfResponse.Create( "", new EwfResponseBodyCreator( () => "" ), additionalHeaderFieldGetter: () => ( "Allow", "" ).ToCollection() )
						.WriteToAspNetResponse( context.Response );
				}
				return;
			}

			EwfResponse response;
			switch( context.Request.HttpMethod ) {
				case "PUT":
					response = put();
					break;
				case "PATCH":
					response = patch();
					break;
				case "DELETE":
					response = delete();
					break;
				case "POST":
					response = post();
					break;
				default:
					context.Response.StatusCode = 501;
					EwfResponse.Create( "", new EwfResponseBodyCreator( () => "" ) ).WriteToAspNetResponse( context.Response );
					return;
			}
			if( response != null )
				response.WriteToAspNetResponse( context.Response );
			else {
				context.Response.StatusCode = 405;
				EwfResponse.Create( "", new EwfResponseBodyCreator( () => "" ), additionalHeaderFieldGetter: () => ( "Allow", "" ).ToCollection() )
					.WriteToAspNetResponse( context.Response );
			}
		}

		/// <summary>
		/// Returns the redirect for the resource, if it is located outside of the application.
		/// </summary>
		protected virtual ExternalRedirect getRedirect() => null;

		private void writeRedirectResponse( HttpContext context, string url, bool permanent ) {
			context.Response.StatusCode = permanent ? 308 : 307;

			if( context.Request.HttpMethod == "GET" || context.Request.HttpMethod == "HEAD" )
				EwfSafeResponseWriter.AddCacheControlHeader(
					context.Response,
					EwfApp.Instance.RequestIsSecure( context.Request ),
					false,
					permanent ? (bool?)null : false );

			EwfResponse.Create(
					ContentTypes.PlainText,
					new EwfResponseBodyCreator( writer => writer.Write( "{0} Redirect: {1}".FormatWith( permanent ? "Permanent" : "Temporary", url ) ) ),
					additionalHeaderFieldGetter: () => ( "Location", url ).ToCollection() )
				.WriteToAspNetResponse( context.Response, omitBody: context.Request.HttpMethod == "HEAD" );
		}

		/// <summary>
		/// Returns the handler for a GET or HEAD request.
		/// </summary>
		protected virtual EwfSafeRequestHandler getOrHead() => null;

		protected virtual EwfResponse put() => null;
		protected virtual EwfResponse patch() => null;
		protected virtual EwfResponse delete() => null;
		protected virtual EwfResponse post() => null;

		protected internal virtual bool AllowsSearchEngineIndexing {
			get {
				if( ParentResource != null )
					return ParentResource.AllowsSearchEngineIndexing;
				if( EsAsBaseType != null )
					return EsAsBaseType.AllowsSearchEngineIndexing;
				return true;
			}
		}

		/// <summary>
		/// Returns true if this resource info object is identical to the current resource info object. The comparison excludes optional parameters that haven't
		/// been explicitly set.
		/// </summary>
		public bool IsIdenticalToCurrent() => Equals( EwfPage.Instance.InfoAsBaseType );

		/// <summary>
		/// Framework use only.
		/// If the type of this resource info object corresponds to the current resource or if the type of this resource info object's entity setup info corresponds
		/// to the current entity setup, this method returns a clone of this object that uses values from the current resource and entity setup instead of defaults
		/// whenever possible.
		/// </summary>
		public abstract ResourceBase CloneAndReplaceDefaultsIfPossible( bool disableReplacementOfDefaults );

		public sealed override bool Equals( object obj ) => Equals( obj as BasicUrlHandler );
		public abstract bool Equals( BasicUrlHandler other );
		public abstract override int GetHashCode();
	}
}