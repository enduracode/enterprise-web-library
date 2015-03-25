using System;
using NUnit.Framework;

namespace EnterpriseWebLibrary.Tests.DateTimeTools {
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
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( today, today, today, today ), false );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( today, today, today, tomorrow ), false );

			/*
			 * affirmative nulls in every combination of 4, 3, 2, 1 nulls
			 * negative nulls in every combination possible (4, 3 nulls shouldn't ever return false, nor should infinity <---> infinity in either pair)
			 * adding years just to make sure that's cool
			 */
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( null, null, null, null ), true );

			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( null, null, null, today ), true );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( null, null, today, null ), true );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( null, today, null, null ), true );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( today, null, null, null ), true );

			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( yesterday, null, null, today ), true );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( yesterday, null, today, null ), true );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( yesterday, today, null, null ), true );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( null, null, yesterday, today ), true );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( null, yesterday, null, today ), true );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( null, today, yesterday, null ), true );

			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( null, tomorrow, yesterday, today ), true );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( today, null, yesterday, tomorrow ), true );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( yesterday, tomorrow, null, today ), true );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( yesterday, tomorrow, today, null ), true );

			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( today, null, null, yesterday ), false );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( null, yesterday, today, null ), false );

			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( null, yesterday, today, tomorrow ), false );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( tomorrow, null, yesterday, today ), false );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( today, tomorrow, null, yesterday ), false );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( yesterday, today, tomorrow, null ), false );

			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( today, today, today.AddYears( 1 ), today.AddYears( 1 ) ), false );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( today, today, today.AddYears( -1 ), today.AddYears( 1 ) ), true );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( today.AddYears( 1 ), today.AddYears( 1 ), today, today ), false );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( today.AddYears( -1 ), today.AddYears( 1 ), today, today ), true );

			Assert.Catch( typeof( ApplicationException ), () => RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( today, today, today, yesterday ) );

			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( today, today, today, null ), false );

			Assert.Catch( typeof( ApplicationException ), () => RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( today, today, tomorrow, today ) );

			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( today, today, yesterday, today ), true );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( today, today, null, today ), true );

			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( today, tomorrow, today, today ), true );
			Assert.Catch( typeof( ApplicationException ), () => RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( today, yesterday, today, today ) );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( today, null, today, today ), true );

			Assert.Catch( typeof( ApplicationException ), () => RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( tomorrow, today, today, today ) );
			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( yesterday, today, today, today ), false );

			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( null, today, today, today ), false );

			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( today, fiveOClock, today, tomorrow ), true );
			Assert.Catch( typeof( ApplicationException ), () => RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( today, tomorrow, today, fiveOClock ) );

			Assert.AreEqual( RedStapler.StandardLibrary.DateTimeTools.DateTimeRangeOverlapsDateRange( fiveOClock.AddHours( -1 ), fiveOClock, today, tomorrow ), true );
		}
	}
}