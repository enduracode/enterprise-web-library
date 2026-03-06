using NodaTime;

namespace EnterpriseWebLibrary;

public static class Clock {
	private static Func<Instant>? currentTimeGetter;
	private static Func<Instant>? transactionTimeGetter;

	internal static void Init( ( Func<Instant> current, Func<Instant> transaction ) timeGetters ) {
		currentTimeGetter = timeGetters.current;
		transactionTimeGetter = timeGetters.transaction;
	}

	internal static Instant GetCurrentTime() => currentTimeGetter!();

	/// <summary>
	/// Gets the time instant for the current user transaction.
	/// </summary>
	public static Instant TransactionTime => transactionTimeGetter!();

	/// <summary>
	/// Returns an unsigned value. Environment.TickCount64 is signed but won't overflow for millions of years.
	/// </summary>
	internal static ulong GetTickCount64() => (ulong)Environment.TickCount64;
}