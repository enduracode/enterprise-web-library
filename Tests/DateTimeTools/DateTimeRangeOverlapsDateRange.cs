using NodaTime;
using NUnit.Framework;

namespace Tests.DateTimeTools;

[ TestFixture ]
public class DateTimeRangeOverlapsDateRange {
	[ Test ]
	public void Test() {
		// We copied the entire contents of this method from the DateRangesOverlap test and changed some expected results. This test could be a lot better if we
		// invest more time.

		var today = Clock.TransactionTime.InZone( DateTimeZoneProviders.Tzdb.GetSystemDefault() ).Date;
		var yesterday = today.PlusDays( -1 );
		var tomorrow = today.PlusDays( 1 );
		var midnight = today.AtMidnight();
		var yesterdayMidnight = yesterday.AtMidnight();
		var tomorrowMidnight = tomorrow.AtMidnight();
		var fiveOClock = today.At( new LocalTime( 17, 0 ) );

		// test all combinations of null, yesterday, today, tomorrow
		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( midnight, midnight, today, today ), Is.EqualTo( false ) );
		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( midnight, midnight, today, tomorrow ), Is.EqualTo( false ) );

		/*
		 * affirmative nulls in every combination of 4, 3, 2, 1 nulls
		 * negative nulls in every combination possible (4, 3 nulls shouldn't ever return false, nor should infinity <---> infinity in either pair)
		 * adding years just to make sure that's cool
		 */
		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( null, null, null, null ), Is.EqualTo( true ) );

		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( null, null, null, today ), Is.EqualTo( true ) );
		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( null, null, today, null ), Is.EqualTo( true ) );
		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( null, midnight, null, null ), Is.EqualTo( true ) );
		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( midnight, null, null, null ), Is.EqualTo( true ) );

		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( yesterdayMidnight, null, null, today ), Is.EqualTo( true ) );
		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( yesterdayMidnight, null, today, null ), Is.EqualTo( true ) );
		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( yesterdayMidnight, midnight, null, null ), Is.EqualTo( true ) );
		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( null, null, yesterday, today ), Is.EqualTo( true ) );
		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( null, yesterdayMidnight, null, today ), Is.EqualTo( true ) );
		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( null, midnight, yesterday, null ), Is.EqualTo( true ) );

		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( null, tomorrowMidnight, yesterday, today ), Is.EqualTo( true ) );
		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( midnight, null, yesterday, tomorrow ), Is.EqualTo( true ) );
		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( yesterdayMidnight, tomorrowMidnight, null, today ), Is.EqualTo( true ) );
		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( yesterdayMidnight, tomorrowMidnight, today, null ), Is.EqualTo( true ) );

		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( midnight, null, null, yesterday ), Is.EqualTo( false ) );
		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( null, yesterdayMidnight, today, null ), Is.EqualTo( false ) );

		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( null, yesterdayMidnight, today, tomorrow ), Is.EqualTo( false ) );
		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( tomorrowMidnight, null, yesterday, today ), Is.EqualTo( false ) );
		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( midnight, tomorrowMidnight, null, yesterday ), Is.EqualTo( false ) );
		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( yesterdayMidnight, midnight, tomorrow, null ), Is.EqualTo( false ) );

		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( midnight, midnight, today.PlusYears( 1 ), today.PlusYears( 1 ) ), Is.EqualTo( false ) );
		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( midnight, midnight, today.PlusYears( -1 ), today.PlusYears( 1 ) ), Is.EqualTo( true ) );
		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( midnight.PlusYears( 1 ), midnight.PlusYears( 1 ), today, today ), Is.EqualTo( false ) );
		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( midnight.PlusYears( -1 ), midnight.PlusYears( 1 ), today, today ), Is.EqualTo( true ) );

		Assert.Catch( typeof( ArgumentException ), () => LocalDateTimeTools.RangeOverlapsDateRange( midnight, midnight, today, yesterday ) );

		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( midnight, midnight, today, null ), Is.EqualTo( false ) );

		Assert.Catch( typeof( ArgumentException ), () => LocalDateTimeTools.RangeOverlapsDateRange( midnight, midnight, tomorrow, today ) );

		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( midnight, midnight, yesterday, today ), Is.EqualTo( true ) );
		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( midnight, midnight, null, today ), Is.EqualTo( true ) );

		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( midnight, tomorrowMidnight, today, today ), Is.EqualTo( true ) );
		Assert.Catch( typeof( ArgumentException ), () => LocalDateTimeTools.RangeOverlapsDateRange( midnight, yesterdayMidnight, today, today ) );
		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( midnight, null, today, today ), Is.EqualTo( true ) );

		Assert.Catch( typeof( ArgumentException ), () => LocalDateTimeTools.RangeOverlapsDateRange( tomorrowMidnight, midnight, today, today ) );
		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( yesterdayMidnight, midnight, today, today ), Is.EqualTo( false ) );

		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( null, midnight, today, today ), Is.EqualTo( false ) );

		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( midnight, fiveOClock, today, tomorrow ), Is.EqualTo( true ) );

		Assert.That( LocalDateTimeTools.RangeOverlapsDateRange( fiveOClock.PlusHours( -1 ), fiveOClock, today, tomorrow ), Is.EqualTo( true ) );
	}
}