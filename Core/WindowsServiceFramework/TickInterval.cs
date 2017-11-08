using System.Collections.Generic;
using System.Linq;
using NodaTime;

namespace EnterpriseWebLibrary.WindowsServiceFramework {
	public sealed class TickInterval {
		private readonly Interval interval;
		private readonly IReadOnlyCollection<(LocalDateTime Beginning, LocalDateTime End)> localIntervals;

		internal TickInterval( Interval interval ) {
			this.interval = interval;
			localIntervals = interval.ToLocalIntervals();
		}

		public bool Contains( Instant instant ) {
			return interval.Contains( instant );
		}

		public bool FitsPattern( OperationRecurrencePattern pattern ) {
			return localIntervals.Any( i => pattern.IntervalFits( i.Beginning, i.End ) );
		}
	}
}