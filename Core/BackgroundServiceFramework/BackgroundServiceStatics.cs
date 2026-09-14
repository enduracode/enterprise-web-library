using NodaTime;

namespace EnterpriseWebLibrary.BackgroundServiceFramework;

public static class BackgroundServiceStatics {
	/// <summary>
	/// Gets the time instant for the current call of <see cref="BackgroundServiceBase.Tick(TickInterval)"/>.
	/// </summary>
	public static Instant TickTime { get; internal set; }
}