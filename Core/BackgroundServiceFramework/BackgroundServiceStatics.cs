using NodaTime;

namespace EnterpriseWebLibrary.BackgroundServiceFramework;

public static class BackgroundServiceStatics {
	/// <summary>
	/// Gets the time instant for the current call of <see cref="BackgroundServiceBase.Tick(TickInterval)"/>.
	/// </summary>
	public static Instant TickTime { get; internal set; }

	/// <summary>
	/// Gets the service container.
	/// </summary>
	public static IServiceProvider Services { get; private set; } = null!;

	internal static void Init( IServiceProvider services ) {
		Services = services;
	}
}