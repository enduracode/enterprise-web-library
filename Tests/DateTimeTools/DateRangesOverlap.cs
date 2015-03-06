using System;
using NUnit.Framework;

namespace EnterpriseWebLibrary.Tests.DateTimeTools {
	[ TestFixture ]
	public class DateRangesOverlap {
		[ Test ]
		public void Test() {
			var today = DateTime.Today;
			var yesterday = today.AddDays( -1 );
			var tomorrow = today.AddDays( 1 );
			var fiveOClock = today.AddHours( 17 );

			// test all combinations of null, yesterday, today, tomorrow
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( today, today, today, today ), true );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( today, today, today, tomorrow ), true );

			/*
			 * affirmative nulls in every combination of 4, 3, 2, 1 nulls
			 * negative nulls in every combination possible (4, 3 nulls shouldn't ever return false, nor should infinity <---> infinity in either pair)
			 * adding years just to make sure that's cool
			 */
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( null, null, null, null ), true );

			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( null, null, null, today ), true );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( null, null, today, null ), true );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( null, today, null, null ), true );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( today, null, null, null ), true );

			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( yesterday, null, null, today ), true );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( yesterday, null, today, null ), true );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( yesterday, today, null, null ), true );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( null, null, yesterday, today ), true );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( null, yesterday, null, today ), true );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( null, today, yesterday, null ), true );

			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( null, tomorrow, yesterday, today ), true );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( today, null, yesterday, tomorrow ), true );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( yesterday, tomorrow, null, today ), true );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( yesterday, tomorrow, today, null ), true );

			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( today, null, null, yesterday ), false );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( null, yesterday, today, null ), false );

			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( null, yesterday, today, tomorrow ), false );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( tomorrow, null, yesterday, today ), false );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( today, tomorrow, null, yesterday ), false );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( yesterday, today, tomorrow, null ), false );

			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( today, today, today.AddYears( 1 ), today.AddYears( 1 ) ), false );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( today, today, today.AddYears( -1 ), today.AddYears( 1 ) ), true );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( today.AddYears( 1 ), today.AddYears( 1 ), today, today ), false );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( today.AddYears( -1 ), today.AddYears( 1 ), today, today ), true );

			Assert.Catch( typeof( ApplicationException ),
			              () => RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( today, today, today, yesterday ),
			              "Range two ends before it begins." );

			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( today, today, today, null ), true );

			Assert.Catch( typeof( ApplicationException ),
			              () => RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( today, today, tomorrow, today ),
			              "Range two before range one begins" ); // Invalid

			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( today, today, yesterday, today ), true );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( today, today, null, today ), true );

			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( today, tomorrow, today, today ), true );
			Assert.Catch( typeof( ApplicationException ),
			              () => RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( today, yesterday, today, today ),
			              "Range two before range one begins" );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( today, null, today, today ), true );

			Assert.Catch( typeof( ApplicationException ),
			              () => RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( tomorrow, today, today, today ),
			              "Range two before range one begins" );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( yesterday, today, today, today ), true );

			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( null, today, today, today ), true );

			Assert.Catch( typeof( ApplicationException ),
			              () => RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( today, fiveOClock, today, tomorrow ),
			              "Range one contains time" );
			Assert.Catch( typeof( ApplicationException ),
			              () => RedStapler.StandardLibrary.DateTimeTools.DateRangesOverlap( today, tomorrow, today, fiveOClock ),
			              "Range two contains time" );
		}
	}
}