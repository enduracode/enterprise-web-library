using System;
using System.Collections.Generic;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

// Parameter: DateTime date

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class TwoWeekCalendarTest: EwfPage {
		protected override void loadData() {
			twoWeek.SetParameters( info.Date, date => parametersModification.Date = date );
			twoWeek.SetToolTipForDay( DateTime.Today, "tool tip test".GetLiteralControl() );

			for( var i = 1; i <= 12; i++ ) {
				var begin = new DateTime( 2009, i, 1 );
				var middle = new DateTime( 2009, i, 15 );
				var end = new DateTime( 2009, i, 27 );
				foreach( var dateTime in new List<DateTime> { begin, middle, end } ) {
					var calendar = new MonthViewCalendar();
					calendar.IsTwoWeekCalendar = true;
					container.Controls.Add( ( "Start date: " + dateTime.WeekBeginDate() ).GetLiteralControl() );
					container.Controls.Add( calendar );
					calendar.SetParameters( dateTime, delegate { } );
					for( var day = -15; day <= DateTime.DaysInMonth( 2009, i ) + 30; day++ ) {
						calendar.AddContentForDay( dateTime.AddDays( day ), "testing".GetLiteralControl() );
						calendar.SetToolTipForDay( dateTime.AddDays( day ), "testing".GetLiteralControl() );
					}
				}
			}
		}
	}
}