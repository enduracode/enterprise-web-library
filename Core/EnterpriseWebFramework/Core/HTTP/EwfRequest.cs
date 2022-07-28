using System;
using System.Web;
using EnterpriseWebLibrary.Configuration;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public class EwfRequest {
		private static AppRequestBaseUrlProvider baseUrlDefaultProvider;
		private static SystemProviderReference<AppRequestBaseUrlProvider> baseUrlProvider;
		private static Func<HttpRequest> currentRequestGetter;

		internal static void Init( SystemProviderReference<AppRequestBaseUrlProvider> baseUrlProvider, Func<HttpRequest> currentRequestGetter ) {
			baseUrlDefaultProvider = new AppRequestBaseUrlProvider();
			EwfRequest.baseUrlProvider = baseUrlProvider;
			EwfRequest.currentRequestGetter = currentRequestGetter;
		}

		internal static AppRequestBaseUrlProvider AppBaseUrlProvider => baseUrlProvider.GetProvider( returnNullIfNotFound: true ) ?? baseUrlDefaultProvider;

		public static EwfRequest Current => new EwfRequest( currentRequestGetter() );

		private readonly HttpRequest aspNetRequest;

		private EwfRequest( HttpRequest aspNetRequest ) {
			this.aspNetRequest = aspNetRequest;
		}

		/// <summary>
		/// Returns true if this request is secure.
		/// </summary>
		public bool IsSecure => AppBaseUrlProvider.RequestIsSecure( aspNetRequest );
	}
}