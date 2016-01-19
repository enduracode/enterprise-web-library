using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// Provides an easy way to build a calendar where data is viewed one month at a time.
	/// </summary>
	public class MonthViewCalendar: WebControl, ControlTreeDataLoader {
		private readonly Dictionary<DateTime, TableCell> daysOfMonthToCells = new Dictionary<DateTime, TableCell>();

		private readonly Dictionary<DateTime, ToolTipProperties> daysOfMonthToolTips = new Dictionary<DateTime, ToolTipProperties>();

		private DateTime date;
		private Action<DateTime> dateModificationMethod;

		/// <summary>
		/// Displays a two week calendar. Events and ToolTips added to the calendar are displayed even if they are not in the current
		/// month with a two week calendar.
		/// </summary>
		public bool IsTwoWeekCalendar { get; set; }

		/// <summary>
		/// Sets the ActionControlStyle for the previous navigation button.
		/// </summary>
		public ActionControlStyle PreviousButton { get; set; }

		/// <summary>
		/// Sets the text for the PreviousButton. If that button happens to be ImageActionControlStyle, sets the AlternateText.
		/// </summary>
		public string PreviousButtonText { get; set; }

		/// <summary>
		/// Sets the ActionControlStyle for the current date nagivation button.
		/// </summary>
		public ActionControlStyle CurrentDateButton { get; set; }

		/// <summary>
		/// Sets the text for the CurrentDateButton. If that button happens to be ImageActionControlStyle, sets the AlternateText.
		/// </summary>
		public string CurrentDateButtonText { get; set; }

		/// <summary>
		/// Sets the ActionControlStyle for the next nagivation button.
		/// </summary>
		public ActionControlStyle NextButton { get; set; }

		/// <summary>
		/// Sets the text for the NextButton. If that button happens to be ImageActionControlStyle, sets the AlternateText.
		/// </summary>
		public string NextButtonText { get; set; }


		/// <summary>
		/// Initializes defaults for MonthViewCalendar attributes.
		/// </summary>
		public MonthViewCalendar() {
			PreviousButtonText = "Previous";
			PreviousButton = new ImageActionControlStyle( new ExternalResourceInfo( "~/Ewf/Images/Calendar/button_previous.gif" ) )
				{
					AlternateText = PreviousButtonText
				};
			CurrentDateButtonText = "Current Date";
			CurrentDateButton =
				new ImageActionControlStyle(
					IsTwoWeekCalendar
						? new ExternalResourceInfo( "~/Ewf/Images/Calendar/button_currentweek.gif" )
						: new ExternalResourceInfo( "~/Ewf/Images/Calendar/button_currentmonth.gif" ) ) { AlternateText = CurrentDateButtonText };


			NextButtonText = "Next";
			NextButton = new ImageActionControlStyle( new ExternalResourceInfo( "~/Ewf/Images/Calendar/button_next.gif" ) ) { AlternateText = NextButtonText };
		}

		/// <summary>
		/// This method must be called in LoadData.
		/// </summary>
		public void SetParameters( DateTime date, Action<DateTime> dateModificationMethod ) {
			this.date = date;
			this.dateModificationMethod = dateModificationMethod;
		}

		/// <summary>
		/// Specifies the content for a particular day of the month.
		/// </summary>
		public void AddContentForDay( DateTime date, Control content ) {
			TableCell dayTableCell;
			if( !daysOfMonthToCells.TryGetValue( date.Date, out dayTableCell ) ) {
				dayTableCell = new TableCell();
				daysOfMonthToCells.Add( date.Date, dayTableCell );
			}
			dayTableCell.Controls.Add( content );
		}

		/// <summary>
		/// Provides the ability to place a tooltip for a day.
		/// </summary>
		public void SetToolTipForDay( DateTime date, Control toolTipContent ) {
			daysOfMonthToolTips[ date.Date ] = new ToolTipProperties { Content = toolTipContent };
		}

		void ControlTreeDataLoader.LoadData() {
			if( dateModificationMethod == null )
				throw new ApplicationException( "In order to place this calendar on a page, you must call SetParameters before the end of LoadData." );
			buildNavigationBox();

			// Begin drawing calendar
			var table = TableOps.CreateUnderlyingTable();
			table.Attributes[ "class" ] = "ewfMonthView ewfMonthViewCalendar";
			base.Controls.Add( table );

			var headerRow = new TableRow();
			foreach( var day in Enum.GetValues( typeof( DayOfWeek ) ) )
				headerRow.Cells.Add( new TableCell { Text = day.ToString(), CssClass = "commonHeader ewfDaysOfWeekHeader" } );
			table.Rows.Add( headerRow );

			var localDate = IsTwoWeekCalendar ? date.WeekBeginDate() : new DateTime( date.Year, date.Month, 1 ).WeekBeginDate();
			var endDrawingDate = IsTwoWeekCalendar
				                     ? localDate.AddDays( 13 )
				                     : new DateTime( date.Year, date.Month, 1 ).AddDays( DateTime.DaysInMonth( date.Year, date.Month ) - 1 );

			do {
				var row = new TableRow();
				foreach( DayOfWeek dayOfWeek in Enum.GetValues( typeof( DayOfWeek ) ) ) {
					var cell = new TableCell { Text = "&nbsp;" };
					if( IsTwoWeekCalendar || localDate.Date.Month.CompareTo( date.Date.Month ) == 0 ) {
						if( daysOfMonthToCells.ContainsKey( localDate ) )
							cell = daysOfMonthToCells[ localDate ];

						if( daysOfMonthToolTips.ContainsKey( localDate ) )
							new ToolTip( daysOfMonthToolTips[ localDate ].Content, cell );
					}
					cell.Attributes.Add( "class", localDate.CompareTo( DateTime.Now.Date ) == 0 ? "ewfToday" : localDate.Month == date.Month ? "ewfDay" : "ewfDaySpacer" );
					cell.Controls.AddAt( 0, new Paragraph( localDate.Day.ToString().GetLiteralControl() ) { CssClass = "dayLabel" } );
					row.Cells.Add( cell );
					localDate = localDate.AddDays( 1 );
				}
				table.Rows.Add( row );
			}
			while( localDate.CompareTo( endDrawingDate ) <= 0 );
		}

		private void buildNavigationBox() {
			var jumpList =
				SelectList.CreateDropDown(
					from i in Enumerable.Range( -3, 7 ) select SelectListItem.Create( i, formatDateTimeForJumpList( adjustDateByNumberOfIntervals( date, i ) ) ),
					0,
					autoPostBack: true );
			jumpList.Width = JumpListWidth;
			var numIntervals = 0;
			EwfPage.Instance.DataUpdate.AddTopValidationMethod( ( pbv, validator ) => numIntervals = jumpList.ValidateAndGetSelectedItemIdInPostBack( pbv, validator ) );
			EwfPage.Instance.DataUpdate.AddModificationMethod( () => dateModificationMethod( adjustDateByNumberOfIntervals( date, numIntervals ) ) );


			var previousLink =
				new PostBackButton(
					PostBack.CreateFull( id: "prev", firstModificationMethod: () => dateModificationMethod( adjustDateByNumberOfIntervals( date, -1 ) ) ),
					PreviousButton,
					usesSubmitBehavior: false );
			var todayLink = new PostBackButton(
				PostBack.CreateFull( id: "today", firstModificationMethod: () => dateModificationMethod( DateTime.Today ) ),
				CurrentDateButton,
				usesSubmitBehavior: false );
			var nextLink =
				new PostBackButton(
					PostBack.CreateFull( id: "next", firstModificationMethod: () => dateModificationMethod( adjustDateByNumberOfIntervals( date, 1 ) ) ),
					NextButton,
					usesSubmitBehavior: false );

			var table = new DynamicTable { CssClass = "calendarViewHeader ewfNavigationBoxHeader", IsStandard = false };
			var navControls = new Panel();
			foreach( var postBackButton in new List<PostBackButton> { previousLink, todayLink, nextLink } )
				navControls.Controls.Add( postBackButton );

			table.AddRow( jumpList, navControls.ToCell( new TableCellSetup( classes: "calendarViewNavButtons".ToSingleElementArray() ) ) );
			Controls.Add( table );
		}

		// CalendarView
		private Unit JumpListWidth { get { return Unit.Pixel( 150 ); } }

		private DateTime adjustDateByNumberOfIntervals( DateTime date, int i ) {
			return IsTwoWeekCalendar ? date.AddDays( 14 * i ) : date.AddMonths( i );
		}

		private string formatDateTimeForJumpList( DateTime date ) {
			return IsTwoWeekCalendar ? date.WeekBeginDate().ToMonthDayYearString() : date.ToString( "MMMM yyyy" );
		}

		/// <summary>
		/// Returns the div tag, which represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Div; } }

		/// <summary>
		/// This exists to pass the tooltip control + interatable boolean together before actually associating them with
		/// their day cells. This wouldn't exist if anonymous types were better than they are.
		/// </summary>
		private class ToolTipProperties {
			public Control Content { get; set; }
		}
	}
}