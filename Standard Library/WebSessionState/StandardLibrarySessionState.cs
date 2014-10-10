using System;
using System.Collections.Generic;
using System.Web;
using RedStapler.StandardLibrary.EnterpriseWebFramework;

namespace RedStapler.StandardLibrary.WebSessionState {
	/// <summary>
	/// Contains and allows access to all data stored in ASP.NET session state.
	/// Do NOT add anything new to this class since we no longer support session state. See Deliberate Omissions: https://enduracode.fogbugz.com/default.asp?W6.
	/// When we're ready to remove this class, we should also disable session state in the Web.config file, although we might want to provide a way for individual
	/// systems to keep it enabled if necessary.
	/// </summary>
	public class StandardLibrarySessionState {
		private readonly List<Tuple<StatusMessageType, string>> statusMessages = new List<Tuple<StatusMessageType, string>>();
		private string clientSideNavigationUrl = "";
		private bool clientSideNavigationInNewWindow;
		private int? clientSideNavigationDelay;

		/// <summary>
		/// EWF use only.
		/// </summary>
		public FullResponse ResponseToSend { get; set; }

		internal EwfPageRequestState EwfPageRequestState { get; set; }

		private StandardLibrarySessionState() {}

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

		internal List<Tuple<StatusMessageType, string>> StatusMessages { get { return statusMessages; } }

		/// <summary>
		/// Adds a client-side redirect command to the response with the specified URL. The redirect should happen as soon as the page is finished loading.
		/// </summary>
		public void SetInstantClientSideNavigation( string url ) {
			SetClientSideNavigation( url, false, null );
		}

		/// <summary>
		/// Adds a client-side redirect command to the response with the specified URL. The redirect should happen the specified number of seconds after the page is
		/// finished loading.
		/// </summary>
		public void SetTimedClientSideNavigation( string url, int numberOfSeconds ) {
			SetClientSideNavigation( url, false, numberOfSeconds );
		}

		internal void SetClientSideNavigation( string url, bool navigateInNewWindow, int? delayInSeconds ) {
			clientSideNavigationUrl = url;
			clientSideNavigationInNewWindow = navigateInNewWindow;
			clientSideNavigationDelay = delayInSeconds;
		}

		internal void GetClientSideNavigationSetup( out string url, out bool navigateInNewWindow, out int? delayInSeconds ) {
			url = clientSideNavigationUrl;
			navigateInNewWindow = clientSideNavigationInNewWindow;
			delayInSeconds = clientSideNavigationDelay;
		}

		internal void ClearClientSideNavigation() {
			clientSideNavigationUrl = "";
		}

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public bool HideBrowserWarningForRemainderOfSession { get; set; }
	}
}