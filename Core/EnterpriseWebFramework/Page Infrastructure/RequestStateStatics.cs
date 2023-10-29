using Microsoft.AspNetCore.Http;
using NodaTime;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.PageInfrastructure;

internal static class RequestStateStatics {
	private static Func<Instant>? requestTimeGetter;
	private static Action<string>? clientSideNewUrlSetter;
	private static Func<IReadOnlyCollection<( StatusMessageType, string )>>? statusMessageGetter;
	private static Action<IEnumerable<( StatusMessageType, string )>>? statusMessageAppender;
	private static Func<int?>? secondaryResponseIdGetter;
	private static Action<int>? secondaryResponseIdSetter;
	private static Func<PageRequestState?>? pageRequestStateGetter;
	private static Func<PageRequestState, PageRequestState>? pageRequestStateSetter;
	private static Action? slowDataModificationNotifier;
	private static Action? requestStateRefresher;
	private static Func<string, string, Action<HttpContext>, string>? continuationRequestStateStorer;

	internal static void Init(
		Func<Instant> requestTimeGetter, Action<string> clientSideNewUrlSetter, Func<IReadOnlyCollection<( StatusMessageType, string )>> statusMessageGetter,
		Action<IEnumerable<( StatusMessageType, string )>> statusMessageAppender, Func<int?> secondaryResponseIdGetter, Action<int> secondaryResponseIdSetter,
		Func<PageRequestState>? pageRequestStateGetter, Func<PageRequestState, PageRequestState>? pageRequestStateSetter, Action slowDataModificationNotifier,
		Action requestStateRefresher, Func<string, string, Action<HttpContext>, string> continuationRequestStateStorer ) {
		RequestStateStatics.requestTimeGetter = requestTimeGetter;

		RequestStateStatics.clientSideNewUrlSetter = clientSideNewUrlSetter;

		RequestStateStatics.statusMessageGetter = statusMessageGetter;
		RequestStateStatics.statusMessageAppender = statusMessageAppender;

		RequestStateStatics.secondaryResponseIdGetter = secondaryResponseIdGetter;
		RequestStateStatics.secondaryResponseIdSetter = secondaryResponseIdSetter;

		RequestStateStatics.pageRequestStateGetter = pageRequestStateGetter;
		RequestStateStatics.pageRequestStateSetter = pageRequestStateSetter;

		RequestStateStatics.slowDataModificationNotifier = slowDataModificationNotifier;

		RequestStateStatics.requestStateRefresher = requestStateRefresher;

		RequestStateStatics.continuationRequestStateStorer = continuationRequestStateStorer;
	}

	public static Instant RequestTime => requestTimeGetter!();

	public static void SetClientSideNewUrl( string url ) => clientSideNewUrlSetter!( url );

	public static IReadOnlyCollection<( StatusMessageType, string )> GetStatusMessages() => statusMessageGetter!();
	public static void AppendStatusMessages( IEnumerable<( StatusMessageType, string )> messages ) => statusMessageAppender!( messages );

	public static int? GetSecondaryResponseId() => secondaryResponseIdGetter!();
	public static void SetSecondaryResponseId( int id ) => secondaryResponseIdSetter!( id );

	public static PageRequestState? GetPageRequestState() => pageRequestStateGetter!();
	public static PageRequestState SetPageRequestState( PageRequestState state ) => pageRequestStateSetter!( state );
	public static PageRequestState ResetPageRequestState() => SetPageRequestState( new PageRequestState( RequestTime, null, null ) );

	public static void NotifyOfSlowDataModification() => slowDataModificationNotifier!();

	public static void RefreshRequestState() => requestStateRefresher!();

	public static string StoreRequestStateForContinuation( string url, string requestMethod, Action<HttpContext> requestHandler ) =>
		continuationRequestStateStorer!( url, requestMethod, requestHandler );
}