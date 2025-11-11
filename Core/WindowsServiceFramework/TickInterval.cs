using JetBrains.Annotations;
using NodaTime;

namespace EnterpriseWebLibrary.WindowsServiceFramework;

[ PublicAPI ]
public sealed class TickInterval {
	private readonly Interval interval;

	internal TickInterval( Interval interval ) {
		this.interval = interval;
	}

	public bool Contains( Instant instant ) => interval.Contains( instant );

	/// <summary>
	/// Returns whether this interval ends after the specified date. Returns false if you specify null for the date.
	/// </summary>
	public bool EndsAfter( LocalDate? date, DateTimeZone timeZone ) => date.HasValue && interval.End.InZone( timeZone ).Date > date.Value;

	/// <summary>
	/// Returns whether this interval ends at a time when applications are normally used.
	/// </summary>
	public bool EndsWithinNormalUseHours( DateTimeZone timeZone ) => !interval.End.InZone( timeZone ).TimeOfDay.IsInNight();

	public bool FitsPattern( OperationRecurrencePattern pattern, DateTimeZone timeZone ) =>
		interval.ToLocalIntervals( timeZone ).Any( i => pattern.IntervalFits( i.Beginning, i.End ) );
}