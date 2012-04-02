using System;
using System.Collections.Generic;
using System.Linq;

namespace RedStapler.StandardLibrary {
	/// <summary>
	/// Provides helpful TimeSpan methods.
	/// </summary>
	public static class TimeSpanTools {
		/// <summary>
		/// Returns the given timespan in the form "3:24" for three hours and twenty-four minutes.
		/// </summary>
		public static string ToHourMinuteString( this TimeSpan timeSpan ) {
			var hours = (int)timeSpan.TotalHours;
			timeSpan = timeSpan - TimeSpan.FromHours( hours );
			var minutes = (int)Math.Round( timeSpan.TotalMinutes );
			return hours + ":" + minutes.ToString( "D2" );
		}

		/// <summary>
		/// Returns the given timespan in the form "3:24:02" for three hours, twenty-four minutes and two seconds.
		/// </summary>
		public static string ToHourMinuteSecondString( this TimeSpan timeSpan ) {
			var hours = (int)timeSpan.TotalHours;
			timeSpan = timeSpan - TimeSpan.FromHours( hours );
			var minutes = (int)timeSpan.TotalMinutes;
			timeSpan = timeSpan - TimeSpan.FromMinutes( minutes );
			return hours + ":" + minutes.ToString( "D2" ) + ":" + ( (int)Math.Round( timeSpan.TotalSeconds ) ).ToString( "D2" );
		}

		/// <summary>
		/// Returns a string such as "52 seconds" or "1 minute 3 seconds" or "4 days, 6 hours".
		/// </summary>
		public static string ToConciseString( this TimeSpan timeSpan ) {
			var results = new List<string>();

			if( timeSpan.TotalDays > 1 )
				results.Add( timeSpan.Days + " days" );
			if( timeSpan.TotalHours > 1 )
				results.Add( timeSpan.Hours + " hours" );
			if( timeSpan.TotalMinutes > 1 )
				results.Add( timeSpan.Minutes + " minutes" );

			results.Add( timeSpan.Seconds + " seconds" );

			return StringTools.ConcatenateWithDelimiter( ", ", results.Take( 2 ).ToArray() );
		}

		/// <summary>
		/// Formats the specified time span as a time of day in hour:minute style followed by a single lowercase letter indicating AM or PM.
		/// </summary>
		public static string ToTimeOfDayHourAndMinuteString( this TimeSpan timeSpan ) {
			return
				new DateTime( DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds ).
					ToHourAndMinuteString();
		}
	}
}