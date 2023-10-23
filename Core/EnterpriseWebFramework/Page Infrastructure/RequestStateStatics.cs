namespace EnterpriseWebLibrary.EnterpriseWebFramework.PageInfrastructure;

internal static class RequestStateStatics {
	internal static Action<string> ClientSideNewUrlSetter = null!;
	internal static Func<IReadOnlyCollection<( StatusMessageType, string )>> StatusMessageGetter = null!;
	internal static Action<IEnumerable<( StatusMessageType, string )>> StatusMessageAppender = null!;
	internal static Func<int?> SecondaryResponseIdGetter = null!;
	internal static Action<int> SecondaryResponseIdSetter = null!;
	internal static Action SlowDataModificationNotifier = null!;
	internal static Action RequestStateRefresher = null!;

	internal static void Init(
		Action<string> clientSideNewUrlSetter, Func<IReadOnlyCollection<( StatusMessageType, string )>> statusMessageGetter,
		Action<IEnumerable<( StatusMessageType, string )>> statusMessageAppender, Func<int?> secondaryResponseIdGetter, Action<int> secondaryResponseIdSetter,
		Action slowDataModificationNotifier, Action requestStateRefresher ) {
		ClientSideNewUrlSetter = clientSideNewUrlSetter;
		StatusMessageGetter = statusMessageGetter;
		StatusMessageAppender = statusMessageAppender;
		SecondaryResponseIdGetter = secondaryResponseIdGetter;
		SecondaryResponseIdSetter = secondaryResponseIdSetter;
		SlowDataModificationNotifier = slowDataModificationNotifier;
		RequestStateRefresher = requestStateRefresher;
	}
}