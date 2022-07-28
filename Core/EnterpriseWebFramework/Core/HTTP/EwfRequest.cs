using System;
using System.Web;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public class EwfRequest {
		private static Func<HttpRequest> currentRequestGetter;

		internal static void Init( Func<HttpRequest> currentRequestGetter ) {
			EwfRequest.currentRequestGetter = currentRequestGetter;
		}

		public static EwfRequest Current => new EwfRequest( currentRequestGetter() );

		private readonly HttpRequest aspNetRequest;

		private EwfRequest( HttpRequest aspNetRequest ) {
			this.aspNetRequest = aspNetRequest;
		}

		/// <summary>
		/// Returns true if this request is secure.
		/// </summary>
		public bool IsSecure => EwfApp.Instance.RequestIsSecure( aspNetRequest );
	}
}