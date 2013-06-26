using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.AlternativePageModes;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A base set of functionality that can be used to discover information about an entity setup before requesting a page that uses it.
	/// </summary>
	public abstract class EntitySetupInfo {
		private bool parentPageLoaded;
		private PageInfo parentPage;
		private ReadOnlyCollection<PageGroup> pages;
		private readonly Lazy<AlternativePageMode> alternativeMode;

		/// <summary>
		/// Creates an entity setup info object.
		/// </summary>
		protected EntitySetupInfo() {
			alternativeMode = new Lazy<AlternativePageMode>( createAlternativeMode );
		}

		/// <summary>
		/// Throws an exception if the parameter values or any non URL elements of the current request make the entity setup invalid.
		/// </summary>
		protected virtual void init() {
			init( DataAccessState.Current.PrimaryDatabaseConnection );
		}

		[ Obsolete( "Guaranteed through 30 September 2013. Please use the overload without the DBConnection parameter." ) ]
		protected virtual void init( DBConnection cn ) {}

		/// <summary>
		/// Creates a page info object for the parent page of this entity setup. Returns null if there is no parent page.
		/// </summary>
		protected abstract PageInfo createParentPageInfo();

		/// <summary>
		/// Creates a list of page groups for the pages that are part of this entity setup.
		/// </summary>
		protected abstract List<PageGroup> createPageInfos();

		/// <summary>
		/// Gets the page info object for the parent page of this entity setup. Returns null if there is no parent page.
		/// </summary>
		public PageInfo ParentPage {
			get {
				if( parentPageLoaded )
					return parentPage;
				parentPageLoaded = true;
				return parentPage = createParentPageInfo();
			}
		}

		/// <summary>
		/// Gets the list of page info objects for the pages that are part of this entity setup.
		/// </summary>
		public ReadOnlyCollection<PageGroup> Pages { get { return pages ?? ( pages = createPageInfos().AsReadOnly() ); } }

		/// <summary>
		/// Returns the name of the entity setup.
		/// </summary>
		public abstract string EntitySetupName { get; }

		/// <summary>
		/// Returns true if the authenticated user passes entity setup authorization checks.
		/// </summary>
		protected internal virtual bool UserCanAccessEntitySetup { get { return true; } }

		/// <summary>
		/// Gets the log in page to use for pages that are part of this entity setup if the system supports forms authentication.
		/// </summary>
		protected internal virtual PageInfo LogInPage { get { return ParentPage != null ? ParentPage.LogInPage : null; } }

		/// <summary>
		/// Gets the alternative mode for this entity setup or null if it is in normal mode. Do not call this from the createAlternativeMode method of an ancestor;
		/// doing so will result in a stack overflow.
		/// </summary>
		public AlternativePageMode AlternativeMode {
			get {
				// It's important to do the parent disabled check first so the entity setup doesn't have to repeat any of it in its disabled check.
				if( ParentPage != null && ParentPage.AlternativeMode is DisabledPageMode )
					return ParentPage.AlternativeMode;
				return AlternativeModeDirect;
			}
		}

		/// <summary>
		/// Gets the alternative mode for this entity setup without using ancestor logic. Useful when called from the createAlternativeMode method of an ancestor,
		/// e.g. when implementing a parent that should have new content when one or more children have new content. When calling this property take care to meet
		/// any preconditions that would normally be handled by ancestor logic.
		/// </summary>
		public AlternativePageMode AlternativeModeDirect { get { return alternativeMode.Value; } }

		/// <summary>
		/// Creates the alternative mode for this entity setup or returns null if it is in normal mode.
		/// </summary>
		protected virtual AlternativePageMode createAlternativeMode() {
			return null;
		}

		/// <summary>
		/// Gets the desired security setting for requests to pages that are part of this entity setup.
		/// </summary>
		protected internal virtual ConnectionSecurity ConnectionSecurity { get { return ParentPage != null ? ParentPage.ConnectionSecurity : ConnectionSecurity.SecureIfPossible; } }
	}
}