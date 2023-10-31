using Microsoft.AspNetCore.Http;

namespace EnterpriseWebLibrary.WebSessionState;

/// <summary>
/// Contains and allows access to all data stored in ASP.NET session state.
/// Do NOT add anything new to this class since we no longer support session state. See Deliberate Omissions: https://enduracode.fogbugz.com/default.asp?W6.
/// When we're ready to remove this class, we should also disable session state in the Web.config file, although we might want to provide a way for individual
/// systems to keep it enabled if necessary.
/// </summary>
internal static class StandardLibrarySessionState {
	private static Func<ISession>? currentSessionGetter;

	internal static void Init( Func<HttpContext> currentContextGetter ) {
		currentSessionGetter = () => currentContextGetter().Session;
	}
}