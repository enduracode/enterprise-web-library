using NUnit.Framework;

namespace Tests.DateTimeTools;

[ TestFixture ]
public class DateTimeRangeOverlapsDateRange {
	[ Test ]
	public void Test() {
		// We copied the entire contents of this method from the DateRangesOverlap test and changed some expected results. This test could be a lot better if we
		// invest more time.

		var today = DateTime.Today;
		var yesterday = today.AddDays( -1 );
		var tomorrow = today.AddDays( 1 );
		var fiveOClock = today.AddHours( 17 );

		// test all combinations of null, yesterday, today, tomorrow
		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( today, today, today, today ), Is.EqualTo( false ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( today, today, today, tomorrow ), Is.EqualTo( false ) );

		/*
		 * affirmative nulls in every combination of 4, 3, 2, 1 nulls
		 * negative nulls in every combination possible (4, 3 nulls shouldn't ever return false, nor should infinity <---> infinity in either pair)
		 * adding years just to make sure that's cool
		 */
		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( null, null, null, null ), Is.EqualTo( true ) );

		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( null, null, null, today ), Is.EqualTo( true ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( null, null, today, null ), Is.EqualTo( true ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( null, today, null, null ), Is.EqualTo( true ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( today, null, null, null ), Is.EqualTo( true ) );

		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( yesterday, null, null, today ), Is.EqualTo( true ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( yesterday, null, today, null ), Is.EqualTo( true ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( yesterday, today, null, null ), Is.EqualTo( true ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( null, null, yesterday, today ), Is.EqualTo( true ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( null, yesterday, null, today ), Is.EqualTo( true ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( null, today, yesterday, null ), Is.EqualTo( true ) );

		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( null, tomorrow, yesterday, today ), Is.EqualTo( true ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( today, null, yesterday, tomorrow ), Is.EqualTo( true ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( yesterday, tomorrow, null, today ), Is.EqualTo( true ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( yesterday, tomorrow, today, null ), Is.EqualTo( true ) );

		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( today, null, null, yesterday ), Is.EqualTo( false ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( null, yesterday, today, null ), Is.EqualTo( false ) );

		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( null, yesterday, today, tomorrow ), Is.EqualTo( false ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( tomorrow, null, yesterday, today ), Is.EqualTo( false ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( today, tomorrow, null, yesterday ), Is.EqualTo( false ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( yesterday, today, tomorrow, null ), Is.EqualTo( false ) );

		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( today, today, today.AddYears( 1 ), today.AddYears( 1 ) ), Is.EqualTo( false ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( today, today, today.AddYears( -1 ), today.AddYears( 1 ) ), Is.EqualTo( true ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( today.AddYears( 1 ), today.AddYears( 1 ), today, today ), Is.EqualTo( false ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( today.AddYears( -1 ), today.AddYears( 1 ), today, today ), Is.EqualTo( true ) );

		Assert.Catch( typeof( ApplicationException ), () => Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( today, today, today, yesterday ) );

		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( today, today, today, null ), Is.EqualTo( false ) );

		Assert.Catch( typeof( ApplicationException ), () => Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( today, today, tomorrow, today ) );

		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( today, today, yesterday, today ), Is.EqualTo( true ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( today, today, null, today ), Is.EqualTo( true ) );

		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( today, tomorrow, today, today ), Is.EqualTo( true ) );
		Assert.Catch( typeof( ApplicationException ), () => Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( today, yesterday, today, today ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( today, null, today, today ), Is.EqualTo( true ) );

		Assert.Catch( typeof( ApplicationException ), () => Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( tomorrow, today, today, today ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( yesterday, today, today, today ), Is.EqualTo( false ) );

		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( null, today, today, today ), Is.EqualTo( false ) );

		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( today, fiveOClock, today, tomorrow ), Is.EqualTo( true ) );
		Assert.Catch( typeof( ApplicationException ), () => Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( today, tomorrow, today, fiveOClock ) );

		Assert.That( Tewl.Tools.DateTimeTools.DateTimeRangeOverlapsDateRange( fiveOClock.AddHours( -1 ), fiveOClock, today, tomorrow ), Is.EqualTo( true ) );
	}
}