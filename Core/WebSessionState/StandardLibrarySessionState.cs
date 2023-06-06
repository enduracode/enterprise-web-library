using System.Collections.Immutable;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace EnterpriseWebLibrary.WebSessionState;

/// <summary>
/// Contains and allows access to all data stored in ASP.NET session state.
/// Do NOT add anything new to this class since we no longer support session state. See Deliberate Omissions: https://enduracode.fogbugz.com/default.asp?W6.
/// When we're ready to remove this class, we should also disable session state in the Web.config file, although we might want to provide a way for individual
/// systems to keep it enabled if necessary.
/// </summary>
internal static class StandardLibrarySessionState {
	private static Func<ISession> currentSessionGetter;

	internal static void Init( Func<HttpContext> currentContextGetter ) {
		currentSessionGetter = () => currentContextGetter().Session;
	}

	internal static IReadOnlyCollection<( StatusMessageType, string )> StatusMessages {
		get {
			var value = currentSessionGetter().GetString( "ewfStatusMessages" );
			return value != null
				       ? JsonConvert.DeserializeObject<ImmutableArray<( StatusMessageType, string )>>( value )
				       : Enumerable.Empty<( StatusMessageType, string )>().Materialize();
		}
		set => currentSessionGetter().SetString( "ewfStatusMessages", JsonConvert.SerializeObject( value, Formatting.None ) );
	}

	internal static void SetClientSideNavigation( string url, bool navigateInNewWindow ) {
		currentSessionGetter().SetString( "ewfClientSideNavigation", JsonConvert.SerializeObject( ( url, navigateInNewWindow ), Formatting.None ) );
	}

	internal static void GetClientSideNavigationSetup( out string url, out bool navigateInNewWindow ) {
		var value = currentSessionGetter().GetString( "ewfClientSideNavigation" );
		if( value != null ) {
			var pair = JsonConvert.DeserializeObject<( string, bool )>( value );
			url = pair.Item1;
			navigateInNewWindow = pair.Item2;
		}
		else {
			url = "";
			navigateInNewWindow = false;
		}
	}

	internal static void ClearClientSideNavigation() {
		currentSessionGetter().Remove( "ewfClientSideNavigation" );
	}

	internal static bool HasResponseToSend => currentSessionGetter().Keys.Contains( "ewfResponseToSend" );

	internal static FullResponse ResponseToSend {
		get {
			var value = currentSessionGetter().GetString( "ewfResponseToSend" );
			return value != null ? JsonConvert.DeserializeObject<FullResponse>( value ) : null;
		}
		set => currentSessionGetter().SetString( "ewfResponseToSend", JsonConvert.SerializeObject( value, Formatting.None ) );
	}
}