using System;
using NodaTime;

namespace EnterpriseWebLibrary.WindowsServiceFramework {
	public sealed class OperationRecurrencePattern {
		/// <summary>
		/// Creates a pattern of hourly recurrence on the specified minute, in local time. "Fall-back" time changes can cause more than 24 occurrences per day.
		/// </summary>
		public static OperationRecurrencePattern CreateHourly( int minute ) {
			return new OperationRecurrencePattern(
				( beginDateTime, endDateTime ) => {
					if( endDateTime - Period.FromHours( 1 ) >= beginDateTime )
						return true;

					var beginTime = beginDateTime.TimeOfDay - Period.FromHours( beginDateTime.Hour );
					var endTime = endDateTime.TimeOfDay - Period.FromHours( endDateTime.Hour );
					var time = new LocalTime( 0, minute );
					return beginTime <= endTime ? beginTime <= time && time < endTime : beginTime <= time || time < endTime;
				} );
		}

		/// <summary>
		/// Creates a pattern of daily recurrence on the specified hour of the day (i.e. 24-hour basis) and minute, in local time. "Fall-back" time changes can
		/// cause more than one occurrence per day.
		/// </summary>
		public static OperationRecurrencePattern CreateDaily( int hour, int minute ) {
			return new OperationRecurrencePattern(
				( beginDateTime, endDateTime ) => {
					if( endDateTime - Period.FromDays( 1 ) >= beginDateTime )
						return true;

					var beginTime = beginDateTime.TimeOfDay;
					var endTime = endDateTime.TimeOfDay;
					var time = new LocalTime( hour, minute );
					return beginTime <= endTime ? beginTime <= time && time < endTime : beginTime <= time || time < endTime;
				} );
		}

		/// <summary>
		/// Creates a pattern of weekly recurrence on the specified day, hour of the day (i.e. 24-hour basis), and minute, in local time. "Fall-back" time changes
		/// can cause more than one occurrence per week.
		/// </summary>
		public static OperationRecurrencePattern CreateWeekly( IsoDayOfWeek day, int hour, int minute ) {
			return new OperationRecurrencePattern(
				( beginDateTime, endDateTime ) => {
					if( endDateTime - Period.FromWeeks( 1 ) >= beginDateTime )
						return true;

					var date = beginDateTime.Date;
					while( date <= endDateTime.Date ) {
						if( date.DayOfWeek == day ) {
							var dayBeginDateTime = date.AtMidnight();
							if( beginDateTime > dayBeginDateTime )
								dayBeginDateTime = beginDateTime;

							var dayEndDateTime = date.PlusDays( 1 ).AtMidnight();
							if( endDateTime < dayEndDateTime )
								dayEndDateTime = endDateTime;

							var time = new LocalTime( hour, minute );
							if( dayBeginDateTime.TimeOfDay <= time && time < dayEndDateTime.TimeOfDay )
								return true;
						}
						date += Period.FromDays( 1 );
					}
					return false;
				} );
		}

		private readonly Func<LocalDateTime, LocalDateTime, bool> predicate;

		private OperationRecurrencePattern( Func<LocalDateTime, LocalDateTime, bool> predicate ) {
			this.predicate = predicate;
		}

		internal bool IntervalFits( LocalDateTime beginDateTime, LocalDateTime endDateTime ) {
			return predicate( beginDateTime, endDateTime );
		}
	}
}