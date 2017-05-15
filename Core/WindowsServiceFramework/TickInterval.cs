using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NodaTime;

namespace EnterpriseWebLibrary.WindowsServiceFramework {
	public sealed class TickInterval {
		private readonly Interval interval;
		private readonly List<Tuple<LocalDateTime, LocalDateTime>> localIntervals;

		internal TickInterval( Interval interval ) {
			this.interval = interval;

			var zone = DateTimeZoneProviders.Tzdb.GetSystemDefault();
			var zoneIntervals = zone.GetZoneIntervals( interval ).ToImmutableArray();
			localIntervals = new List<Tuple<LocalDateTime, LocalDateTime>>();
			for( var i = 0; i < zoneIntervals.Length; i += 1 ) {
				var beginDateTime = i == 0 ? interval.Start.InZone( zone ).LocalDateTime : zoneIntervals[ i ].IsoLocalStart;
				if( i == zoneIntervals.Length - 1 )
					localIntervals.Add( Tuple.Create( beginDateTime, interval.End.InZone( zone ).LocalDateTime ) );
				else {
					localIntervals.Add( Tuple.Create( beginDateTime, zoneIntervals[ i ].IsoLocalEnd ) );

					// If there is skipped time between the zone intervals, add an interval for it.
					if( zoneIntervals[ i + 1 ].IsoLocalStart > zoneIntervals[ i ].IsoLocalEnd )
						localIntervals.Add( Tuple.Create( zoneIntervals[ i ].IsoLocalEnd, zoneIntervals[ i + 1 ].IsoLocalStart ) );
				}
			}
		}

		public bool Contains( Instant instant ) {
			return interval.Contains( instant );
		}

		public bool FitsPattern( OperationRecurrencePattern pattern ) {
			return localIntervals.Any( i => pattern.IntervalFits( i.Item1, i.Item2 ) );
		}
	}
}