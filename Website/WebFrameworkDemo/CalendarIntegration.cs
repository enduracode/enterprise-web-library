using NodaTime;

// EwlPage

namespace EnterpriseWebLibrary.Website.WebFrameworkDemo;

partial class CalendarIntegration {
	protected override PageContent getContent() =>
		new UiPageContent( omitContentBox: true ).Add( new Section( "Normal event", getNormalEvent().ToCollection(), style: SectionStyle.Box ) )
			.Add( new Section( "All-day event", getAllDayEvent().ToCollection(), style: SectionStyle.Box ) );

	private FlowComponent getNormalEvent() {
		const string eventTitle = "Tomorrow’s meeting";
		var timeZone = DateTimeZoneProviders.Tzdb.GetSystemDefault();
		var date = FirstRequestTime.InZone( timeZone ).Date.PlusDays( 1 );
		var beginDateAndTime = date.At( new LocalTime( 10, 0 ) ).InZoneStrictly( timeZone );
		var endDateAndTime = date.At( new LocalTime( 11, 0 ) ).InZoneStrictly( timeZone );
		var description = "This is just a test." + Environment.NewLine + "It’s not a real meeting.";

		return FormItemList
			.CreateStack(
				generalSetup: new FormItemListSetup(
					buttonSetup: new ButtonSetup(
						"Add to my calendar",
						behavior: CalendarIntegrationStatics.GetAddToCalendarButtonBehavior( eventTitle, beginDateAndTime, endDateAndTime, description: description ),
						icon: new ActionComponentIcon( new FontAwesomeIcon( "fa-calendar" ) ) ) ) )
			.AddItem( eventTitle.ToFormItem( label: "Event title".ToComponents() ) )
			.AddItem(
				$"{date.ToDateTimeUnspecified().ToDayMonthYearString( false )}, {beginDateAndTime.ToDateTimeUnspecified().ToHourAndMinuteString()} to {endDateAndTime.ToDateTimeUnspecified().ToHourAndMinuteString()}"
					.ToFormItem( label: "Date and time".ToComponents() ) )
			.AddItem( description.ToFormItem( label: "Description".ToComponents() ) );
	}

	private FlowComponent getAllDayEvent() {
		const string eventTitle = "Upcoming vacation";
		var beginDate = FirstRequestTime.InZone( DateTimeZoneProviders.Tzdb.GetSystemDefault() ).Date.Next( IsoDayOfWeek.Monday );
		var endDate = beginDate.Next( IsoDayOfWeek.Friday );
		var description = "This is just a test." + Environment.NewLine + "It’s not a real vacation, unfortunately.";

		return FormItemList
			.CreateStack(
				generalSetup: new FormItemListSetup(
					buttonSetup: new ButtonSetup(
						"Add to my calendar",
						behavior: CalendarIntegrationStatics.GetAddToCalendarButtonBehaviorForAllDayEvent( eventTitle, beginDate, endDate, description: description ),
						icon: new ActionComponentIcon( new FontAwesomeIcon( "fa-calendar" ) ) ) ) )
			.AddItem( eventTitle.ToFormItem( label: "Event title".ToComponents() ) )
			.AddItem(
				$"{beginDate.ToDateTimeUnspecified().ToDayMonthYearString( false )} to {endDate.ToDateTimeUnspecified().ToDayMonthYearString( false )}".ToFormItem(
					label: "Date".ToComponents() ) )
			.AddItem( description.ToFormItem( label: "Description".ToComponents() ) );
	}
}