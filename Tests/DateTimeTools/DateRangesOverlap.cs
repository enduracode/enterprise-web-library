using NodaTime;
using NUnit.Framework;

namespace Tests.DateTimeTools;

[ TestFixture ]
public class DateRangesOverlap {
	[ Test ]
	public void Test() {
		var today = Clock.TransactionTime.InZone( DateTimeZoneProviders.Tzdb.GetSystemDefault() ).Date;
		var yesterday = today.PlusDays( -1 );
		var tomorrow = today.PlusDays( 1 );

		// test all combinations of null, yesterday, today, tomorrow
		Assert.That( LocalDateTools.RangesOverlap( today, today, today, today ), Is.EqualTo( true ) );
		Assert.That( LocalDateTools.RangesOverlap( today, today, today, tomorrow ), Is.EqualTo( true ) );

		/*
		 * affirmative nulls in every combination of 4, 3, 2, 1 nulls
		 * negative nulls in every combination possible (4, 3 nulls shouldn't ever return false, nor should infinity <---> infinity in either pair)
		 * adding years just to make sure that's cool
		 */
		Assert.That( LocalDateTools.RangesOverlap( null, null, null, null ), Is.EqualTo( true ) );

		Assert.That( LocalDateTools.RangesOverlap( null, null, null, today ), Is.EqualTo( true ) );
		Assert.That( LocalDateTools.RangesOverlap( null, null, today, null ), Is.EqualTo( true ) );
		Assert.That( LocalDateTools.RangesOverlap( null, today, null, null ), Is.EqualTo( true ) );
		Assert.That( LocalDateTools.RangesOverlap( today, null, null, null ), Is.EqualTo( true ) );

		Assert.That( LocalDateTools.RangesOverlap( yesterday, null, null, today ), Is.EqualTo( true ) );
		Assert.That( LocalDateTools.RangesOverlap( yesterday, null, today, null ), Is.EqualTo( true ) );
		Assert.That( LocalDateTools.RangesOverlap( yesterday, today, null, null ), Is.EqualTo( true ) );
		Assert.That( LocalDateTools.RangesOverlap( null, null, yesterday, today ), Is.EqualTo( true ) );
		Assert.That( LocalDateTools.RangesOverlap( null, yesterday, null, today ), Is.EqualTo( true ) );
		Assert.That( LocalDateTools.RangesOverlap( null, today, yesterday, null ), Is.EqualTo( true ) );

		Assert.That( LocalDateTools.RangesOverlap( null, tomorrow, yesterday, today ), Is.EqualTo( true ) );
		Assert.That( LocalDateTools.RangesOverlap( today, null, yesterday, tomorrow ), Is.EqualTo( true ) );
		Assert.That( LocalDateTools.RangesOverlap( yesterday, tomorrow, null, today ), Is.EqualTo( true ) );
		Assert.That( LocalDateTools.RangesOverlap( yesterday, tomorrow, today, null ), Is.EqualTo( true ) );

		Assert.That( LocalDateTools.RangesOverlap( today, null, null, yesterday ), Is.EqualTo( false ) );
		Assert.That( LocalDateTools.RangesOverlap( null, yesterday, today, null ), Is.EqualTo( false ) );

		Assert.That( LocalDateTools.RangesOverlap( null, yesterday, today, tomorrow ), Is.EqualTo( false ) );
		Assert.That( LocalDateTools.RangesOverlap( tomorrow, null, yesterday, today ), Is.EqualTo( false ) );
		Assert.That( LocalDateTools.RangesOverlap( today, tomorrow, null, yesterday ), Is.EqualTo( false ) );
		Assert.That( LocalDateTools.RangesOverlap( yesterday, today, tomorrow, null ), Is.EqualTo( false ) );

		Assert.That( LocalDateTools.RangesOverlap( today, today, today.PlusYears( 1 ), today.PlusYears( 1 ) ), Is.EqualTo( false ) );
		Assert.That( LocalDateTools.RangesOverlap( today, today, today.PlusYears( -1 ), today.PlusYears( 1 ) ), Is.EqualTo( true ) );
		Assert.That( LocalDateTools.RangesOverlap( today.PlusYears( 1 ), today.PlusYears( 1 ), today, today ), Is.EqualTo( false ) );
		Assert.That( LocalDateTools.RangesOverlap( today.PlusYears( -1 ), today.PlusYears( 1 ), today, today ), Is.EqualTo( true ) );

		Assert.Catch( typeof( ArgumentException ), () => LocalDateTools.RangesOverlap( today, today, today, yesterday ), "Range two ends before it begins." );

		Assert.That( LocalDateTools.RangesOverlap( today, today, today, null ), Is.EqualTo( true ) );

		Assert.Catch(
			typeof( ArgumentException ),
			() => LocalDateTools.RangesOverlap( today, today, tomorrow, today ),
			"Range two before range one begins" ); // Invalid

		Assert.That( LocalDateTools.RangesOverlap( today, today, yesterday, today ), Is.EqualTo( true ) );
		Assert.That( LocalDateTools.RangesOverlap( today, today, null, today ), Is.EqualTo( true ) );

		Assert.That( LocalDateTools.RangesOverlap( today, tomorrow, today, today ), Is.EqualTo( true ) );
		Assert.Catch( typeof( ArgumentException ), () => LocalDateTools.RangesOverlap( today, yesterday, today, today ), "Range two before range one begins" );
		Assert.That( LocalDateTools.RangesOverlap( today, null, today, today ), Is.EqualTo( true ) );

		Assert.Catch( typeof( ArgumentException ), () => LocalDateTools.RangesOverlap( tomorrow, today, today, today ), "Range two before range one begins" );
		Assert.That( LocalDateTools.RangesOverlap( yesterday, today, today, today ), Is.EqualTo( true ) );

		Assert.That( LocalDateTools.RangesOverlap( null, today, today, today ), Is.EqualTo( true ) );
	}
}