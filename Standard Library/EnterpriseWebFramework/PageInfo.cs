using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.AlternativePageModes;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A base set of functionality that can be used to discover information about a page before actually requesting it.
	/// </summary>
	public abstract class PageInfo {
		internal const string PagePathSeparator = " > ";
		internal const string EntityPageSeparator = " / ";

		internal static string CombinePagePathStrings( string separator, string one, string two, params string[] pathStrings ) {
			var pathString = StringTools.ConcatenateWithDelimiter( separator, one, two );
			foreach( var s in pathStrings )
				pathString = StringTools.ConcatenateWithDelimiter( separator, pathString, s );
			return pathString;
		}

		private string uriFragmentIdentifierField = "";
		private readonly Lazy<PageInfo> parentPage;
		private readonly Lazy<AlternativePageMode> alternativeMode;

		/// <summary>
		/// Creates a page info object.
		/// </summary>
		protected PageInfo() {
			parentPage = new Lazy<PageInfo>( createParentPageInfo );
			alternativeMode = new Lazy<AlternativePageMode>( createAlternativeMode );
		}

		/// <summary>
		/// Throws an exception if the parameter values or any non URL elements of the current request make the page invalid.
		/// </summary>
		protected abstract void init( DBConnection cn );

		/// <summary>
		/// Executes the specified fragment identifier validator, which takes the page's fragment identifier and should return a non empty string, i.e. an error
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
		/// Standard Library and auto-generated code use only.
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
		/// Gets the entity setup info object if one exists.
		/// </summary>
		public abstract EntitySetupInfo EsInfoAsBaseType { get; }

		/// <summary>
		/// Gets the page info object for the parent page of this page. Returns null if there is no parent page.
		/// </summary>
		public PageInfo ParentPage { get { return parentPage.Value; } }

		/// <summary>
		/// Creates a page info object for the parent page of this page. Returns null if there is no parent page.
		/// </summary>
		protected virtual PageInfo createParentPageInfo() {
			return null;
		}

		/// <summary>
		/// Gets the page path from the root all the way down to the current page.
		/// </summary>
		public List<PageInfo> PagePath {
			get {
				// NOTE: If we used recursion this would be much simpler.
				var path = new List<PageInfo>();
				var page = this;
				do {
					path.Add( page );
				}
				while( ( page = page.ParentPage ?? ( page.EsInfoAsBaseType != null ? page.EsInfoAsBaseType.ParentPage : null ) ) != null );
				path.Reverse();
				return path;
			}
		}

		/// <summary>
		/// Returns the name of the page.
		/// </summary>
		public virtual string PageName { get { return GetType().DeclaringType.Name.CamelToEnglish(); } }

		/// <summary>
		/// Returns the name of the page, including the entity setup name if an entity setup exists.
		/// </summary>
		public string PageFullName { get { return CombinePagePathStrings( EntityPageSeparator, EsInfoAsBaseType != null && ParentPage == null ? EsInfoAsBaseType.EntitySetupName : "", PageName ); } }

		/// <summary>
		/// Returns the string representing the parent page's path within the entity.
		/// </summary>
		internal string ParentPageEntityPathString {
			get {
				if( EsInfoAsBaseType == null )
					return "";
				var pathString = "";
				for( var i = 0; i < PagePath.Count - 1; i += 1 ) {
					// Traverse the path backwards, excluding this page.
					var page = PagePath[ PagePath.Count - 2 - i ];

					// NOTE: There should be a third part of this condition that tests if all the parameters in the entity setups are the same.
					if( page.EsInfoAsBaseType == null || !page.EsInfoAsBaseType.GetType().Equals( EsInfoAsBaseType.GetType() ) )
						break;

					pathString = CombinePagePathStrings( PagePathSeparator, page.PageFullName, pathString );
				}
				return pathString;
			}
		}

		/// <summary>
		/// Returns true if the authenticated user is authorized to access everything on the page.
		/// </summary>
		public bool UserCanAccessPageAndAllControls {
			get {
				if( AppTools.IsIntermediateInstallation ) {
					if( IsIntermediateInstallationPublicPage )
						return true;
					if( !AppRequestState.Instance.IntermediateUserExists )
						return false;
				}

				// It's important to do the entity setup check first so the page doesn't have to repeat any of it. For example, if the entity setup verifies that the
				// authenticated user is not null, the page should be able to assume this when doing its own checks.
				return ( EsInfoAsBaseType != null
					         ? ( EsInfoAsBaseType.ParentPage != null ? EsInfoAsBaseType.ParentPage.UserCanAccessPageAndAllControls : true ) &&
					           EsInfoAsBaseType.UserCanAccessEntitySetup
					         : true ) && ( ParentPage != null ? ParentPage.UserCanAccessPageAndAllControls : true ) && userCanAccessPage;
			}
		}

		/// <summary>
		/// Gets whether the page is public in intermediate installations.
		/// </summary>
		protected internal virtual bool IsIntermediateInstallationPublicPage { get { return false; } }

		/// <summary>
		/// Returns true if the authenticated user passes page-level authorization checks.
		/// </summary>
		protected virtual bool userCanAccessPage { get { return true; } }

		/// <summary>
		/// Gets the log in page to use for this page if the system supports forms authentication.
		/// </summary>
		protected internal virtual PageInfo LogInPage {
			get {
				if( ParentPage != null )
					return ParentPage.LogInPage;
				if( EsInfoAsBaseType != null )
					return EsInfoAsBaseType.LogInPage;
				return null;
			}
		}

		/// <summary>
		/// Gets the alternative mode for this page or null if it is in normal mode. Do not call this from the createAlternativeMode method of an ancestor; doing so
		/// will result in a stack overflow.
		/// </summary>
		public AlternativePageMode AlternativeMode {
			get {
				// It's important to do the entity setup and parent disabled checks first so the page doesn't have to repeat any of them in its disabled check.
				if( EsInfoAsBaseType != null && EsInfoAsBaseType.AlternativeMode is DisabledPageMode )
					return EsInfoAsBaseType.AlternativeMode;
				if( ParentPage != null && ParentPage.AlternativeMode is DisabledPageMode )
					return ParentPage.AlternativeMode;
				return AlternativeModeDirect;
			}
		}

		/// <summary>
		/// Gets the alternative mode for this page without using ancestor logic. Useful when called from the createAlternativeMode method of an ancestor, e.g. when
		/// implementing a parent that should have new content when one or more children have new content. When calling this property take care to meet any
		/// preconditions that would normally be handled by ancestor logic.
		/// </summary>
		public AlternativePageMode AlternativeModeDirect { get { return alternativeMode.Value; } }

		/// <summary>
		/// Creates the alternative mode for this page or returns null if it is in normal mode.
		/// </summary>
		protected virtual AlternativePageMode createAlternativeMode() {
			return null;
		}

		/// <summary>
		/// Returns an absolute URL that can be used to request the page. This method uses HttpContext.Current and can only be called from within web requests.
		/// </summary>
		/// <param name="disableAuthorizationCheck">Pass true to allow a URL to be returned that the authenticated user cannot access. Use with caution. Might be
		/// useful if you are adding the URL to an email message or otherwise displaying it outside the application.</param>
		/// <returns></returns>
		public string GetUrl( bool disableAuthorizationCheck = false ) {
			// App relative URLs can be a problem when stored in returnUrl query parameters or otherwise stored across requests since a stored page might have a
			// different security level than the current page, and when redirecting to the stored page we wouldn't switch. Therefore we always use absolute URLs.
			return GetUrl( !disableAuthorizationCheck, true, true );
		}

		internal string GetUrl( bool ensureUserCanAccessPage, bool ensurePageNotDisabled, bool makeAbsolute ) {
			var url = buildUrl() + uriFragmentIdentifier.PrependDelimiter( "#" );
			if( ensureUserCanAccessPage && !UserCanAccessPageAndAllControls )
				throw new ApplicationException( "GetUrl was called for a page that the authenticated user cannot access. The URL would have been " + url + "." );
			if( ensurePageNotDisabled && AlternativeMode is DisabledPageMode )
				throw new ApplicationException( "GetUrl was called for a page that is disabled. The URL would have been " + url + "." );
			if( makeAbsolute )
				url = url.Replace( "~", AppRequestState.Instance.GetBaseUrlWithSpecificSecurity( ShouldBeSecureGivenCurrentRequest ) );
			return url;
		}

		/// <summary>
		/// Returns a URL that can be used to request the page. Does not validate query parameters.
		/// </summary>
		protected abstract string buildUrl();

		internal bool ShouldBeSecureGivenCurrentRequest { get { return ConnectionSecurity.ShouldBeSecureGivenCurrentRequest( IsIntermediateInstallationPublicPage ); } }

		/// <summary>
		/// Gets the desired security setting for requests to the page.
		/// </summary>
		protected internal virtual ConnectionSecurity ConnectionSecurity {
			get {
				if( ParentPage != null )
					return ParentPage.ConnectionSecurity;
				if( EsInfoAsBaseType != null )
					return EsInfoAsBaseType.ConnectionSecurity;
				return ConnectionSecurity.SecureIfPossible;
			}
		}

		/// <summary>
		/// Returns true if this page info object is identical to the current page info object. The comparison excludes optional parameters that haven't been
		/// explicitly set.
		/// </summary>
		public bool IsIdenticalToCurrent() {
			return isIdenticalTo( EwfPage.Instance.InfoAsBaseType );
		}

		/// <summary>
		/// Returns true if this page info object is identical to the specified page info object.
		/// </summary>
		protected abstract bool isIdenticalTo( PageInfo infoAsBaseType );

		/// <summary>
		/// If the type of this page info object corresponds to the current page or if the type of this page info object's entity setup info corresponds to the
		/// current entity setup, this method returns a clone of this object that uses values from the current page and entity setup instead of defaults whenever
		/// possible.
		/// </summary>
		protected internal abstract PageInfo CloneAndReplaceDefaultsIfPossible( bool disableReplacementOfDefaults );
	}
}