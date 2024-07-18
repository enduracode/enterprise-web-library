using JetBrains.Annotations;
using NodaTime;

namespace EnterpriseWebLibrary.WindowsServiceFramework;

[ PublicAPI ]
public sealed class TickInterval {
	private readonly Interval interval;
	private readonly IReadOnlyCollection<(LocalDateTime Beginning, LocalDateTime End)> localIntervals;

	internal TickInterval( Interval interval ) {
		this.interval = interval;
		localIntervals = interval.ToLocalIntervals();
	}

	public bool Contains( Instant instant ) => interval.Contains( instant );

	/// <summary>
	/// Returns whether this interval ends after the specified date. Returns false if you specify null for the date.
	/// </summary>
	public bool EndsAfter( LocalDate? date ) => date.HasValue && interval.End.InZone( DateTimeZoneProviders.Tzdb.GetSystemDefault() ).Date > date.Value;

	/// <summary>
	/// Returns whether this interval ends at a time when applications are normally used.
	/// </summary>
	public bool EndsWithinNormalUseHours() => !interval.End.InZone( DateTimeZoneProviders.Tzdb.GetSystemDefault() ).TimeOfDay.IsInNight();

	public bool FitsPattern( OperationRecurrencePattern pattern ) => localIntervals.Any( i => pattern.IntervalFits( i.Beginning, i.End ) );
}