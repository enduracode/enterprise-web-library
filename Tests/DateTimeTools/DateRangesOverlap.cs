using System;
using NUnit.Framework;

namespace EnterpriseWebLibrary.Tests.DateTimeTools;

[ TestFixture ]
public class DateRangesOverlap {
	[ Test ]
	public void Test() {
		var today = DateTime.Today;
		var yesterday = today.AddDays( -1 );
		var tomorrow = today.AddDays( 1 );
		var fiveOClock = today.AddHours( 17 );

		// test all combinations of null, yesterday, today, tomorrow
		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( today, today, today, today ), Is.EqualTo( true ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( today, today, today, tomorrow ), Is.EqualTo( true ) );

		/*
		 * affirmative nulls in every combination of 4, 3, 2, 1 nulls
		 * negative nulls in every combination possible (4, 3 nulls shouldn't ever return false, nor should infinity <---> infinity in either pair)
		 * adding years just to make sure that's cool
		 */
		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( null, null, null, null ), Is.EqualTo( true ) );

		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( null, null, null, today ), Is.EqualTo( true ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( null, null, today, null ), Is.EqualTo( true ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( null, today, null, null ), Is.EqualTo( true ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( today, null, null, null ), Is.EqualTo( true ) );

		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( yesterday, null, null, today ), Is.EqualTo( true ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( yesterday, null, today, null ), Is.EqualTo( true ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( yesterday, today, null, null ), Is.EqualTo( true ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( null, null, yesterday, today ), Is.EqualTo( true ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( null, yesterday, null, today ), Is.EqualTo( true ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( null, today, yesterday, null ), Is.EqualTo( true ) );

		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( null, tomorrow, yesterday, today ), Is.EqualTo( true ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( today, null, yesterday, tomorrow ), Is.EqualTo( true ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( yesterday, tomorrow, null, today ), Is.EqualTo( true ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( yesterday, tomorrow, today, null ), Is.EqualTo( true ) );

		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( today, null, null, yesterday ), Is.EqualTo( false ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( null, yesterday, today, null ), Is.EqualTo( false ) );

		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( null, yesterday, today, tomorrow ), Is.EqualTo( false ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( tomorrow, null, yesterday, today ), Is.EqualTo( false ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( today, tomorrow, null, yesterday ), Is.EqualTo( false ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( yesterday, today, tomorrow, null ), Is.EqualTo( false ) );

		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( today, today, today.AddYears( 1 ), today.AddYears( 1 ) ), Is.EqualTo( false ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( today, today, today.AddYears( -1 ), today.AddYears( 1 ) ), Is.EqualTo( true ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( today.AddYears( 1 ), today.AddYears( 1 ), today, today ), Is.EqualTo( false ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( today.AddYears( -1 ), today.AddYears( 1 ), today, today ), Is.EqualTo( true ) );

		Assert.Catch(
			typeof( ApplicationException ),
			() => Tewl.Tools.DateTimeTools.DateRangesOverlap( today, today, today, yesterday ),
			"Range two ends before it begins." );

		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( today, today, today, null ), Is.EqualTo( true ) );

		Assert.Catch(
			typeof( ApplicationException ),
			() => Tewl.Tools.DateTimeTools.DateRangesOverlap( today, today, tomorrow, today ),
			"Range two before range one begins" ); // Invalid

		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( today, today, yesterday, today ), Is.EqualTo( true ) );
		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( today, today, null, today ), Is.EqualTo( true ) );

		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( today, tomorrow, today, today ), Is.EqualTo( true ) );
		Assert.Catch(
			typeof( ApplicationException ),
			() => Tewl.Tools.DateTimeTools.DateRangesOverlap( today, yesterday, today, today ),
			"Range two before range one begins" );
		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( today, null, today, today ), Is.EqualTo( true ) );

		Assert.Catch(
			typeof( ApplicationException ),
			() => Tewl.Tools.DateTimeTools.DateRangesOverlap( tomorrow, today, today, today ),
			"Range two before range one begins" );
		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( yesterday, today, today, today ), Is.EqualTo( true ) );

		Assert.That( Tewl.Tools.DateTimeTools.DateRangesOverlap( null, today, today, today ), Is.EqualTo( true ) );

		Assert.Catch(
			typeof( ApplicationException ),
			() => Tewl.Tools.DateTimeTools.DateRangesOverlap( today, fiveOClock, today, tomorrow ),
			"Range one contains time" );
		Assert.Catch(
			typeof( ApplicationException ),
			() => Tewl.Tools.DateTimeTools.DateRangesOverlap( today, tomorrow, today, fiveOClock ),
			"Range two contains time" );
	}
}