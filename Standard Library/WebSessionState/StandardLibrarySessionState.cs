using System;
using System.Collections.Generic;
using System.Web;
using RedStapler.StandardLibrary.EnterpriseWebFramework;

namespace RedStapler.StandardLibrary.WebSessionState {
	/// <summary>
	/// Contains and allows access to all data stored in ASP.NET session state.
	/// </summary>
	public class StandardLibrarySessionState {
		private readonly List<Tuple<StatusMessageType, string>> statusMessages = new List<Tuple<StatusMessageType, string>>();
		private string clientSideRedirectUrl = "";
		private bool clientSideRedirectInNewWindow;
		private int? clientSideRedirectDelay;

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
		public void SetInstantClientSideRedirect( string url ) {
			SetClientSideRedirect( url, false, null );
		}

		/// <summary>
		/// Adds a client-side redirect command to the response with the specified URL. The redirect should happen the specified number of seconds after the page is
		/// finished loading.
		/// </summary>
		public void SetTimedClientSideRedirect( string url, int numberOfSeconds ) {
			SetClientSideRedirect( url, false, numberOfSeconds );
		}

		internal void SetClientSideRedirect( string url, bool navigateInNewWindow, int? delayInSeconds ) {
			clientSideRedirectUrl = url;
			clientSideRedirectInNewWindow = navigateInNewWindow;
			clientSideRedirectDelay = delayInSeconds;
		}

		internal void GetClientSideRedirectUrlAndDelay( out string url, out bool navigateInNewWindow, out int? delayInSeconds ) {
			url = clientSideRedirectUrl;
			navigateInNewWindow = clientSideRedirectInNewWindow;
			delayInSeconds = clientSideRedirectDelay;
		}

		internal void ClearClientSideRedirectUrlAndDelay() {
			clientSideRedirectUrl = "";
			clientSideRedirectDelay = null;
		}

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public bool HideBrowserWarningForRemainderOfSession { get; set; }
	}
}