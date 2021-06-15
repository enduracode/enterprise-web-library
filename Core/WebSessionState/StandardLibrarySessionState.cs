using System;
using System.Collections.Generic;
using System.Web;
using EnterpriseWebLibrary.EnterpriseWebFramework;

namespace EnterpriseWebLibrary.WebSessionState {
	/// <summary>
	/// Contains and allows access to all data stored in ASP.NET session state.
	/// Do NOT add anything new to this class since we no longer support session state. See Deliberate Omissions: https://enduracode.fogbugz.com/default.asp?W6.
	/// When we're ready to remove this class, we should also disable session state in the Web.config file, although we might want to provide a way for individual
	/// systems to keep it enabled if necessary.
	/// </summary>
	internal class StandardLibrarySessionState {
		// This is a hack that exists because sometimes error pages are processed at a point in the life cycle when session state is not available.
		public static bool SessionAvailable => HttpContext.Current.Session != null;

		/// <summary>
		/// Returns the session state object for the current HTTP context.
		/// </summary>
		public static StandardLibrarySessionState Instance {
			get {
				if( HttpContext.Current.Session[ "StandardLibrarySessionStateObject" ] == null )
					HttpContext.Current.Session[ "StandardLibrarySessionStateObject" ] = new StandardLibrarySessionState();
				return HttpContext.Current.Session[ "StandardLibrarySessionStateObject" ] as StandardLibrarySessionState;
			}
		}

		private readonly List<Tuple<StatusMessageType, string>> statusMessages = new List<Tuple<StatusMessageType, string>>();
		private string clientSideNavigationUrl = "";
		private bool clientSideNavigationInNewWindow;

		/// <summary>
		/// EWF use only.
		/// </summary>
		public FullResponse ResponseToSend { get; set; }

		internal EwfPageRequestState EwfPageRequestState { get; set; }

		private StandardLibrarySessionState() {}

		internal List<Tuple<StatusMessageType, string>> StatusMessages { get { return statusMessages; } }

		internal void SetClientSideNavigation( string url, bool navigateInNewWindow ) {
			clientSideNavigationUrl = url;
			clientSideNavigationInNewWindow = navigateInNewWindow;
		}

		internal void GetClientSideNavigationSetup( out string url, out bool navigateInNewWindow ) {
			url = clientSideNavigationUrl;
			navigateInNewWindow = clientSideNavigationInNewWindow;
		}

		internal void ClearClientSideNavigation() {
			clientSideNavigationUrl = "";
		}

		/// <summary>
		/// EWL use only.
		/// </summary>
		public bool HideBrowserWarningForRemainderOfSession { get; set; }
	}
}