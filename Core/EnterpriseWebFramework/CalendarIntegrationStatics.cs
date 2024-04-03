using System.Net;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using NodaTime;
using NodaTime.Text;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

public static class CalendarIntegrationStatics {
	public static ButtonBehavior GetAddToCalendarButtonBehavior(
		string eventTitle, ZonedDateTime beginDateAndTime, ZonedDateTime endDateAndTime, string description = "" ) =>
		getButtonBehavior(
			eventTitle,
			() => getICalendarResponse( eventTitle, beginDateAndTime, endDateAndTime, description ),
			getHyperlinks( eventTitle, beginDateAndTime, endDateAndTime, description ) );

	public static ButtonBehavior
		GetAddToCalendarButtonBehaviorForAllDayEvent( string eventTitle, LocalDate beginDate, LocalDate endDate, string description = "" ) =>
		getButtonBehavior(
			eventTitle,
			() => getICalendarResponseForAllDayEvent( eventTitle, beginDate, endDate, description ),
			getHyperlinksForAllDayEvent( eventTitle, beginDate, endDate, description ) );

	private static ButtonBehavior getButtonBehavior( string eventTitle, Func<EwfResponse> iCalendarResponseGetter, IEnumerable<EwfHyperlink> hyperlinks ) =>
		new MenuButtonBehavior(
			new Paragraph(
					new EwfButton(
							new StandardButtonStyle( "Get iCal (.ics) file", buttonSize: ButtonSize.ShrinkWrap ),
							behavior: new PostBackBehavior(
								postBack: PostBack.CreateIntermediate(
									null,
									id: PostBack.GetCompositeId( eventTitle, "downloadIcs" ),
									reloadBehaviorGetter: () => new PageReloadBehavior( secondaryResponse: new SecondaryResponse( iCalendarResponseGetter ) ) ) ) )
						.Append<PhrasingComponent>( new LineBreak() )
						.Append( new SideComments( "For import into any calendar".ToComponents() ) )
						.Materialize() ).Append( new Paragraph( new ImportantContent( "Or add event to:".ToComponents() ).ToCollection() ) )
				.Append<FlowComponent>( new StackList( hyperlinks.Select( i => i.ToComponentListItem() ) ) )
				.Materialize(),
			menuTitle: "Please choose an option" );

	private static EwfResponse getICalendarResponse( string eventTitle, ZonedDateTime beginDateAndTime, ZonedDateTime endDateAndTime, string description ) {
		var calendar = new Calendar();

		var e = new CalendarEvent { Summary = eventTitle, Start = getICalendarTime( beginDateAndTime ), End = getICalendarTime( endDateAndTime ) };
		if( description.Length > 0 )
			e.Description = description;
		calendar.Events.Add( e );

		return EwfResponse.Create(
			"text/calendar",
			new EwfResponseBodyCreator( () => new CalendarSerializer().SerializeToString( calendar ) ),
			fileNameCreator: () => "Event.ics" );
	}

	private static CalDateTime getICalendarTime( ZonedDateTime time ) =>
		new( time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second, time.Zone.Id );

	private static EwfResponse getICalendarResponseForAllDayEvent( string eventTitle, LocalDate beginDate, LocalDate endDate, string description ) {
		var calendar = new Calendar();

		var e = new CalendarEvent { Summary = eventTitle, Start = getICalendarDate( beginDate ), End = getICalendarDate( endDate ), IsAllDay = true };
		if( description.Length > 0 )
			e.Description = description;
		calendar.Events.Add( e );

		return EwfResponse.Create(
			"text/calendar",
			new EwfResponseBodyCreator( () => new CalendarSerializer().SerializeToString( calendar ) ),
			fileNameCreator: () => "Event.ics" );
	}

	private static CalDateTime getICalendarDate( LocalDate date ) => new( date.Year, date.Month, date.Day );

	private static IEnumerable<EwfHyperlink> getHyperlinks(
		string eventTitle, ZonedDateTime beginDateAndTime, ZonedDateTime endDateAndTime, string description ) {
		var timeZone = beginDateAndTime.Zone;
		if( !string.Equals( endDateAndTime.Zone.Id, beginDateAndTime.Zone.Id ) )
			throw new Exception( "The begin and end times must use the same time zone." );

		var encodedTitle = WebUtility.UrlEncode( eventTitle );
		var encodedZone = timeZone.Id;
		var timePattern = LocalDateTimePattern.CreateWithInvariantCulture( "uuuuMMdd'T'HHmmss" );
		var encodedBegin = timePattern.Format( beginDateAndTime.LocalDateTime );
		var encodedEnd = timePattern.Format( endDateAndTime.LocalDateTime );
		var encodedDesc = WebUtility.UrlEncode( description );

		// see https://interactiondesignfoundation.github.io/add-event-to-calendar-docs/services/google.html and https://stackoverflow.com/q/22757908/35349
		yield return new EwfHyperlink(
			new ExternalResource(
				$"https://calendar.google.com/calendar/render?action=TEMPLATE&text={encodedTitle}&ctz={encodedZone}&dates={encodedBegin}/{encodedEnd}" +
				( description.Length > 0 ? $"&details={encodedDesc}" : "" ) ).ToHyperlinkNewTabBehavior(),
			new StandardHyperlinkStyle( "Google Calendar" ) );

		// see https://interactiondesignfoundation.github.io/add-event-to-calendar-docs/services/outlook-web.html and
		// https://webapps.stackexchange.com/questions/95250/office365-query-strings-and-url-parameters and https://www.labnol.org/calendar/
		var outlookBegin = WebUtility.UrlEncode( LocalDateTimePattern.GeneralIso.Format( beginDateAndTime.WithZone( DateTimeZone.Utc ).LocalDateTime ) ) + "Z";
		var outlookEnd = WebUtility.UrlEncode( LocalDateTimePattern.GeneralIso.Format( endDateAndTime.WithZone( DateTimeZone.Utc ).LocalDateTime ) ) + "Z";
		yield return new EwfHyperlink(
			new ExternalResource(
				$"https://outlook.office.com/calendar/action/compose?rru=addevent&subject={encodedTitle}&startdt={outlookBegin}&enddt={outlookEnd}" +
				( description.Length > 0 ? $"&body={WebUtility.UrlEncode( description.GetTextAsEncodedHtml() )}" : "" ) ).ToHyperlinkNewTabBehavior(),
			new StandardHyperlinkStyle( "Outlook" ) );

		// see https://interactiondesignfoundation.github.io/add-event-to-calendar-docs/services/yahoo.html and
		// https://interactiondesignfoundation.github.io/add-event-to-calendar-docs/services/aol.html
		yield return new EwfHyperlink(
			new ExternalResource(
					$"https://calendar.yahoo.com/?v=60&title={encodedTitle}&st={encodedBegin}&et={encodedEnd}" +
					( description.Length > 0 ? $"&desc={encodedDesc}" : "" ) )
				.ToHyperlinkNewTabBehavior(),
			new StandardHyperlinkStyle( "Yahoo Calendar" ) );
		yield return new EwfHyperlink(
			new ExternalResource(
					$"https://calendar.aol.com/?v=60&title={encodedTitle}&st={encodedBegin}&et={encodedEnd}" + ( description.Length > 0 ? $"&desc={encodedDesc}" : "" ) )
				.ToHyperlinkNewTabBehavior(),
			new StandardHyperlinkStyle( "AOL Calendar" ) );
	}

	private static IEnumerable<EwfHyperlink> getHyperlinksForAllDayEvent( string eventTitle, LocalDate beginDate, LocalDate endDate, string description ) {
		var encodedTitle = WebUtility.UrlEncode( eventTitle );
		var datePattern = LocalDatePattern.CreateWithInvariantCulture( "uuuuMMdd" );
		var encodedDesc = WebUtility.UrlEncode( description );

		// see https://interactiondesignfoundation.github.io/add-event-to-calendar-docs/services/google.html and https://stackoverflow.com/q/22757908/35349
		yield return new EwfHyperlink(
			new ExternalResource(
				$"https://calendar.google.com/calendar/render?action=TEMPLATE&text={encodedTitle}&dates={datePattern.Format( beginDate )}/{datePattern.Format( endDate.PlusDays( 1 ) )}" +
				( description.Length > 0 ? $"&details={encodedDesc}" : "" ) ).ToHyperlinkNewTabBehavior(),
			new StandardHyperlinkStyle( "Google Calendar" ) );

		// see https://interactiondesignfoundation.github.io/add-event-to-calendar-docs/services/outlook-web.html and
		// https://webapps.stackexchange.com/questions/95250/office365-query-strings-and-url-parameters and https://www.labnol.org/calendar/
		yield return new EwfHyperlink(
			new ExternalResource(
				$"https://outlook.office.com/calendar/action/compose?rru=addevent&subject={encodedTitle}&startdt={LocalDatePattern.Iso.Format( beginDate )}&enddt={LocalDatePattern.Iso.Format( endDate.PlusDays( 1 ) )}&allday=true" +
				( description.Length > 0 ? $"&body={WebUtility.UrlEncode( description.GetTextAsEncodedHtml() )}" : "" ) ).ToHyperlinkNewTabBehavior(),
			new StandardHyperlinkStyle( "Outlook" ) );

		// see https://interactiondesignfoundation.github.io/add-event-to-calendar-docs/services/yahoo.html and
		// https://interactiondesignfoundation.github.io/add-event-to-calendar-docs/services/aol.html
		yield return new EwfHyperlink(
			new ExternalResource(
				$"https://calendar.yahoo.com/?v=60&title={encodedTitle}&st={datePattern.Format( beginDate )}&et={datePattern.Format( endDate )}&dur=allday" +
				( description.Length > 0 ? $"&desc={encodedDesc}" : "" ) ).ToHyperlinkNewTabBehavior(),
			new StandardHyperlinkStyle( "Yahoo Calendar" ) );
		yield return new EwfHyperlink(
			new ExternalResource(
				$"https://calendar.aol.com/?v=60&title={encodedTitle}&st={datePattern.Format( beginDate )}&et={datePattern.Format( endDate )}&dur=allday" +
				( description.Length > 0 ? $"&desc={encodedDesc}" : "" ) ).ToHyperlinkNewTabBehavior(),
			new StandardHyperlinkStyle( "AOL Calendar" ) );
	}
}