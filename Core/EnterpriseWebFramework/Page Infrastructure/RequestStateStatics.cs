using Microsoft.AspNetCore.Http;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.PageInfrastructure;

internal static class RequestStateStatics {
	private static Action<string>? clientSideNewUrlSetter;
	private static Func<IReadOnlyCollection<( StatusMessageType, string )>>? statusMessageGetter;
	private static Action<IEnumerable<( StatusMessageType, string )>>? statusMessageAppender;
	private static Func<uint?>? secondaryResponseIdGetter;
	private static Action<uint>? secondaryResponseIdSetter;
	private static Action? slowDataModificationNotifier;
	private static Action? requestStateRefresher;
	private static Func<string, string, Action<HttpContext>, string>? continuationRequestStateStorer;

	internal static void Init(
		Action<string> clientSideNewUrlSetter, Func<IReadOnlyCollection<( StatusMessageType, string )>> statusMessageGetter,
		Action<IEnumerable<( StatusMessageType, string )>> statusMessageAppender, Func<uint?> secondaryResponseIdGetter, Action<uint> secondaryResponseIdSetter,
		Action slowDataModificationNotifier, Action requestStateRefresher, Func<string, string, Action<HttpContext>, string> continuationRequestStateStorer ) {
		RequestStateStatics.clientSideNewUrlSetter = clientSideNewUrlSetter;

		RequestStateStatics.statusMessageGetter = statusMessageGetter;
		RequestStateStatics.statusMessageAppender = statusMessageAppender;

		RequestStateStatics.secondaryResponseIdGetter = secondaryResponseIdGetter;
		RequestStateStatics.secondaryResponseIdSetter = secondaryResponseIdSetter;

		RequestStateStatics.slowDataModificationNotifier = slowDataModificationNotifier;

		RequestStateStatics.requestStateRefresher = requestStateRefresher;

		RequestStateStatics.continuationRequestStateStorer = continuationRequestStateStorer;
	}

	public static void SetClientSideNewUrl( string url ) => clientSideNewUrlSetter!( url );

	public static IReadOnlyCollection<( StatusMessageType, string )> GetStatusMessages() => statusMessageGetter!();
	public static void AppendStatusMessages( IEnumerable<( StatusMessageType, string )> messages ) => statusMessageAppender!( messages );

	public static uint? GetSecondaryResponseId() => secondaryResponseIdGetter!();
	public static void SetSecondaryResponseId( uint id ) => secondaryResponseIdSetter!( id );

	public static void NotifyOfSlowDataModification() => slowDataModificationNotifier!();

	public static void RefreshRequestState() => requestStateRefresher!();

	public static string StoreRequestStateForContinuation( string url, string requestMethod, Action<HttpContext> requestHandler ) =>
		continuationRequestStateStorer!( url, requestMethod, requestHandler );
}