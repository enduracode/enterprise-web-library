using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal static class EwfApp {
		private static Func<HttpContext> currentContextGetter;

		internal static void Init( Func<HttpContext> currentContextGetter ) {
			EwfApp.currentContextGetter = currentContextGetter;
		}

		/// <summary>
		/// Returns the request-state object for the current HTTP context.
		/// </summary>
		internal static AppRequestState RequestState =>
			currentContextGetter != null /* i.e. IsWebApp */ && currentContextGetter() != null ? RequestDispatchingStatics.RequestState : null;

		internal static async Task EnsureUrlResolved( HttpContext context, RequestDelegate next ) {
			if( context.GetEndpoint() == null )
				throw new ResourceNotAvailableException( "Failed to resolve the URL.", null );
			await next( context );
		}
	}
}