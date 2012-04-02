using System;
using System.Collections.Generic;
using System.Linq;

namespace RedStapler.StandardLibrary {
	/// <summary>
	/// Provides helpful DateTime methods.
	/// </summary>
	public static class DateTimeTools {
		internal static readonly string[] MonthDayYearFormats = new[] { monthDayYearFormat, "MM/dd/yy" };
		internal const string HourAndMinuteFormat = "h:mmt";

		private const string monthDayYearFormat = "MM/dd/yyyy";

		/// <summary>
		/// Formats the date portion of the specified date/time in "day month year" style, e.g. 5 Apr 2008. Returns stringIfNull if the
		/// specified date/time is null.
		/// </summary>
		public static string ToDayMonthYearString( this DateTime? dateTime, string stringIfNull, bool useLeadingZero ) {
			return dateTime.HasValue ? ToDayMonthYearString( dateTime.Value, useLeadingZero ) : stringIfNull;
		}

		/// <summary>
		/// Formats the date portion of the specified date/time in "day month year" style, e.g. 5 Apr 2008.
		/// </summary>
		public static string ToDayMonthYearString( this DateTime dateTime, bool useLeadingZero ) {
			return dateTime.ToString( ( useLeadingZero ? "dd" : "d" ) + " MMM yyyy" );
		}

		/// <summary>
		/// Formats the date portion of the specified date/time in "01/01/2001" style. Returns stringIfNull if the
		/// specified date/time is null.
		/// </summary>
		public static string ToMonthDayYearString( this DateTime? dateTime, string stringIfNull ) {
			return dateTime.HasValue ? ToMonthDayYearString( dateTime.Value ) : stringIfNull;
		}

		/// <summary>
		/// Formats the date portion of the specified date/time in "01/01/2001" style.
		/// </summary>
		public static string ToMonthDayYearString( this DateTime dateTime ) {
			return dateTime.ToString( monthDayYearFormat );
		}

		/// <summary>
		/// Formats the time portion of the specified date/time in hour:minute style followed by a single lowercase letter indicating AM or PM. Returns stringIfNull
		/// if the specified date/time is null.
		/// </summary>
		public static string ToHourAndMinuteString( this DateTime? dateTime, string stringIfNull ) {
			return dateTime.HasValue ? ToHourAndMinuteString( dateTime.Value ) : stringIfNull;
		}

		/// <summary>
		/// Formats the time portion of the specified date/time in hour:minute style followed by a single lowercase letter indicating AM or PM.
		/// </summary>
		public static string ToHourAndMinuteString( this DateTime dateTime ) {
			return dateTime.ToString( HourAndMinuteFormat ).ToLower();
		}

		/// <summary>
		/// Returns the date that the given week starts on.
		/// </summary>
		public static DateTime WeekBeginDate( this DateTime dateTime ) {
			return dateTime.AddDays( -(int)dateTime.DayOfWeek ).Date;
		}

		/// <summary>
		/// Returns true if this date is in between the given DateTimes (inclusive at beginning of range, exclusive at end of range).
		/// Passing null for either of the two dates is considered to be infinity in that direction.
		/// Therefore, passing null for both dates will always result in true.
		/// </summary>
		public static bool IsBetweenDateTimes( this DateTime dateTime, DateTime? onOrAfterDate, DateTime? onOrBeforeDate ) {
			return ( onOrAfterDate == null || dateTime >= onOrAfterDate ) && ( onOrBeforeDate == null || dateTime < onOrBeforeDate );
		}

		/// <summary>
		/// Returns true if this date is in between (inclusive) the given dates.
		/// This method differs from IsBetweenDateTimes in that onOrAfterDate and onOrBeforeDate must be dates only
		/// (an exception will be thrown if time information is passed - use .Date if you have to.)
		/// and that it is inclusive on both ends of the range.
		/// This method also correctly returns true in the case where the dateTime parameter is 3:30PM on 11/15/09 and the onOrBeforeDate is 11/15/09.
		/// If you want to define a range with time information, use IsBetweenDateTimes instead.
		/// Passing null for either of the two dates is considered to be infinity in that direction. Therefore, passing null for both dates will always result in true.
		/// </summary>
		public static bool IsBetweenDates( this DateTime dateTime, DateTime? onOrAfterDate, DateTime? onOrBeforeDate ) {
			if( ( onOrAfterDate.HasValue && onOrAfterDate.Value.TimeOfDay.TotalMilliseconds > 0 ) ||
			    ( onOrBeforeDate.HasValue && onOrBeforeDate.Value.TimeOfDay.TotalMilliseconds > 0 ) )
				throw new ApplicationException( "Date range contains time information." );

			if( onOrBeforeDate.HasValue )
				onOrBeforeDate = onOrBeforeDate.Value.AddDays( 1 );
			return IsBetweenDateTimes( dateTime, onOrAfterDate, onOrBeforeDate );
		}

		/// <summary>
		/// Returns true if the two given date ranges overlap. Passing null for any date means infinity in that direction.
		/// Throws an exception if any of the given dates contains time information. Use .Date if you have to.
		/// See documentation for IsBetweenDates for more information on the date ranges.
		/// </summary>
		public static bool DateRangesOverlap( DateTime? rangeOneBegin, DateTime? rangeOneEnd, DateTime? rangeTwoBegin, DateTime? rangeTwoEnd ) {
			return ( rangeOneBegin.HasValue && rangeOneBegin.Value.IsBetweenDates( rangeTwoBegin, rangeTwoEnd ) ) ||
			       ( rangeOneEnd.HasValue && rangeOneEnd.Value.IsBetweenDates( rangeTwoBegin, rangeTwoEnd ) ) ||
			       ( rangeTwoBegin.HasValue && rangeTwoBegin.Value.IsBetweenDates( rangeOneBegin, rangeOneEnd ) ) ||
			       ( rangeTwoEnd.HasValue && rangeTwoEnd.Value.IsBetweenDates( rangeOneBegin, rangeOneEnd ) );
		}

		/// <summary>
		/// Returns true if the two given DateTime ranges overlap. Passing null for any date means infinity in that direction.
		/// See documentation for IsBetweenDateTimes for more information on the date ranges.
		/// </summary>
		public static bool DateTimeRangesOverlap( DateTime? rangeOneBegin, DateTime? rangeOneEnd, DateTime? rangeTwoBegin, DateTime? rangeTwoEnd ) {
			return ( rangeOneBegin.HasValue && rangeOneBegin.Value.IsBetweenDateTimes( rangeTwoBegin, rangeTwoEnd ) ) ||
			       ( rangeOneEnd.HasValue && rangeOneEnd.Value.IsBetweenDateTimes( rangeTwoBegin, rangeTwoEnd ) ) ||
			       ( rangeTwoBegin.HasValue && rangeTwoBegin.Value.IsBetweenDateTimes( rangeOneBegin, rangeOneEnd ) ) ||
			       ( rangeTwoEnd.HasValue && rangeTwoEnd.Value.IsBetweenDateTimes( rangeOneBegin, rangeOneEnd ) );
		}

		/// <summary>
		/// Returns true when each day between begin and end dates is represented inside one of the date ranges in dateRanges, inclusive.
		/// Be sure time information is not included. No begin date may be after an end date.
		/// </summary>
		public static bool DateRangesCoverAllDates( DateTime beginDate, DateTime endDate, IEnumerable<Tuple<DateTime?, DateTime?>> dateRanges ) {
			for( var day = beginDate; day <= endDate; day = day.AddDays( 1 ) ) {
				if( !dateRanges.Any( dr => day.IsBetweenDates( dr.Item1, dr.Item2 ) ) )
					return false;
			}
			return true;
		}
	}
}