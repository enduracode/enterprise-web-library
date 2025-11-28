using System.Runtime.InteropServices;
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
	/// Returns an unsigned value, which makes more sense than the signed value returned by <see cref="Environment.TickCount64"/>.
	/// </summary>
	[ DllImport( "kernel32" ) ]
	internal static extern ulong GetTickCount64();
}