using System;
using System.Collections.Generic;
using System.Web;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.WebFileSending;

namespace RedStapler.StandardLibrary.WebSessionState {
	/// <summary>
	/// Contains and allows access to all data stored in ASP.NET session state.
	/// </summary>
	public class StandardLibrarySessionState {
		private readonly List<Tuple<StatusMessageType, string>> statusMessages = new List<Tuple<StatusMessageType, string>>();
		private string clientSideRedirectUrl = "";
		private int? clientSideRedirectDelay;
		private FileToBeSent fileToDownload;
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

		/// <summary>
		/// Do not use.
		/// </summary>
		public static void AddStatusMessage( StatusMessageType type, string messageHtml ) {
			EwfPage.AddStatusMessage( type, messageHtml );
		}

		internal List<Tuple<StatusMessageType, string>> StatusMessages { get { return statusMessages; } }

		/// <summary>
		/// Adds a client-side redirect command to the response with the specified URL. The redirect should happen as soon as the page is finished loading.
		/// </summary>
		public void SetInstantClientSideRedirect( string url ) {
			clientSideRedirectUrl = url;
			clientSideRedirectDelay = null;
		}

		/// <summary>
		/// Adds a client-side redirect command to the response with the specified URL. The redirect should happen the specified number of seconds after the page is
		/// finished loading.
		/// </summary>
		public void SetTimedClientSideRedirect( string url, int numberOfSeconds ) {
			clientSideRedirectUrl = url;
			clientSideRedirectDelay = numberOfSeconds;
		}

		internal void GetClientSideRedirectUrlAndDelay( out string url, out int? numberOfSeconds ) {
			url = clientSideRedirectUrl;
			numberOfSeconds = clientSideRedirectDelay;
		}

		internal void ClearClientSideRedirectUrlAndDelay() {
			clientSideRedirectUrl = "";
			clientSideRedirectDelay = null;
		}

		/// <summary>
		/// Framework use only.
		/// </summary>
		public FileToBeSent FileToBeDownloaded {
			get { return fileToDownload; }
			set {
				// It's important that we set the fileToDownload first, because creating GetFile's info object will result in a call to the getter of this property.
				fileToDownload = value;
				SetInstantClientSideRedirect( EwfApp.MetaLogicFactory.CreateGetFilePageInfo().GetUrl() );
			}
		}

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public bool HideBrowserWarningForRemainderOfSession { get; set; }
	}
}