namespace EnterpriseWebLibrary.EnterpriseWebFramework.PageInfrastructure;

internal static class RequestStateStatics {
	private static Action<string>? clientSideNewUrlSetter;
	private static Func<IReadOnlyCollection<( StatusMessageType, string )>>? statusMessageGetter;
	private static Action<IEnumerable<( StatusMessageType, string )>>? statusMessageAppender;
	private static Func<int?>? secondaryResponseIdGetter;
	private static Action<int>? secondaryResponseIdSetter;
	private static Action? slowDataModificationNotifier;
	private static Action? requestStateRefresher;

	internal static void Init(
		Action<string> clientSideNewUrlSetter, Func<IReadOnlyCollection<( StatusMessageType, string )>> statusMessageGetter,
		Action<IEnumerable<( StatusMessageType, string )>> statusMessageAppender, Func<int?> secondaryResponseIdGetter, Action<int> secondaryResponseIdSetter,
		Action slowDataModificationNotifier, Action requestStateRefresher ) {
		RequestStateStatics.clientSideNewUrlSetter = clientSideNewUrlSetter;
		RequestStateStatics.statusMessageGetter = statusMessageGetter;
		RequestStateStatics.statusMessageAppender = statusMessageAppender;
		RequestStateStatics.secondaryResponseIdGetter = secondaryResponseIdGetter;
		RequestStateStatics.secondaryResponseIdSetter = secondaryResponseIdSetter;
		RequestStateStatics.slowDataModificationNotifier = slowDataModificationNotifier;
		RequestStateStatics.requestStateRefresher = requestStateRefresher;
	}

	public static void SetClientSideNewUrl( string url ) => clientSideNewUrlSetter!( url );
	public static IReadOnlyCollection<( StatusMessageType, string )> GetStatusMessages() => statusMessageGetter!();
	public static void AppendStatusMessages( IEnumerable<( StatusMessageType, string )> messages ) => statusMessageAppender!( messages );
	public static int? GetSecondaryResponseId() => secondaryResponseIdGetter!();
	public static void SetSecondaryResponseId( int id ) => secondaryResponseIdSetter!( id );
	public static void NotifyOfSlowDataModification() => slowDataModificationNotifier!();
	public static void RefreshRequestState() => requestStateRefresher!();
}