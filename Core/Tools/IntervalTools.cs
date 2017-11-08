using System.Collections.Generic;
using System.Collections.Immutable;
using NodaTime;

namespace EnterpriseWebLibrary {
	public static class IntervalTools {
		/// <summary>
		/// Returns one or more local intervals that correspond to this interval.
		/// </summary>
		public static IReadOnlyCollection<( LocalDateTime Beginning, LocalDateTime End )> ToLocalIntervals( this Interval interval ) {
			var zone = DateTimeZoneProviders.Tzdb.GetSystemDefault();
			var zoneIntervals = zone.GetZoneIntervals( interval ).ToImmutableArray();
			var localIntervals = new List<(LocalDateTime, LocalDateTime)>();
			for( var i = 0; i < zoneIntervals.Length; i += 1 ) {
				var beginDateTime = i == 0 ? interval.Start.InZone( zone ).LocalDateTime : zoneIntervals[ i ].IsoLocalStart;
				if( i == zoneIntervals.Length - 1 )
					localIntervals.Add( ( beginDateTime, interval.End.InZone( zone ).LocalDateTime ) );
				else {
					localIntervals.Add( ( beginDateTime, zoneIntervals[ i ].IsoLocalEnd ) );

					// If there is skipped time between the zone intervals, add an interval for it.
					if( zoneIntervals[ i + 1 ].IsoLocalStart > zoneIntervals[ i ].IsoLocalEnd )
						localIntervals.Add( ( zoneIntervals[ i ].IsoLocalEnd, zoneIntervals[ i + 1 ].IsoLocalStart ) );
				}
			}
			return localIntervals;
		}
	}
}