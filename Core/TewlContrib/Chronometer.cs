using NodaTime;

namespace EnterpriseWebLibrary.TewlContrib;

/// <summary>
/// Provides a way to measure duration. The timer begins as soon as the object is created. Elapsed reports how much time has elasped since creation. The timer
/// never stops.
/// </summary>
public class Chronometer {
	private readonly Instant created = SystemClock.Instance.GetCurrentInstant();

	/// <summary>
	/// Returns the time elasped since this object was created.
	/// </summary>
	public Duration Elapsed => SystemClock.Instance.GetCurrentInstant() - created;
}