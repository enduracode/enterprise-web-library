using System.Text.Json;
using System.Text.Json.Nodes;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.DataAccess.CommandWriting.Commands;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;
using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure;
using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.ComponentDisplay;
using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.ElementBase.Classification;
using EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic;
using EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic.AlternativeResourceModes;
using EnterpriseWebLibrary.ExternalFunctionality;
using NodaTime;
using NodaTime.Text;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Admin;

// EwlPage
partial class DebugLog {
	private static readonly InstantPattern dbTimePattern = InstantPattern.CreateWithInvariantCulture( "uuuu'-'MM'-'dd'T'HH':'mm':'ss;FFFFFFFFF" );

	private static readonly ZonedDateTimePattern timePattern = ZonedDateTimePattern.CreateWithInvariantCulture( "MMM'-'dd HH:mm:ss.fff", null );

	private static readonly ZonedDateTimePattern timePatternWithOffset =
		ZonedDateTimePattern.CreateWithInvariantCulture( "MMM'-'dd HH:mm:ss.fff '(UTC'o<+H>')'", null );

	private DateTimeZone timeZone;
	private Offset currentOffset;

	protected override void init() {
		timeZone = DateTimeZoneProviders.Tzdb.GetSystemDefault();
		currentOffset = timeZone.GetUtcOffset( EwfRequest.Current!.RequestTime );
	}

	protected override ResourceParent? createParent() => new DiagnosticLog( Es );

	protected override string getResourceName() => "Two-Day Debug Log";

	protected override AlternativeResourceMode? createAlternativeMode() =>
		ExternalFunctionalityStatics.SqliteFunctionalityEnabled ? null : new DisabledResourceMode( "The SQLite provider must be available." );

	protected internal override bool IsSlow => true;

	protected override PageContent getContent() {
		var events = new List<( string time, string level, string message, string properties )>( 1000 );

		var connection = new DatabaseConnection( new SqliteInfo( "Debug Log", EwfConfigurationStatics.AppConfiguration.DebugLogFilePath ) );
		connection.ExecuteWithConnectionOpen( () => {
			var command = new InlineSelect( [ "*" ], "FROM Events", false, orderByClause: "ORDER BY Id DESC" );
			command.Execute(
				connection,
				reader => {
					while( reader.Read() )
						events.Add( ( (string)reader.GetValue( 1 ), (string)reader.GetValue( 2 ), (string)reader.GetValue( 4 ), (string)reader.GetValue( 5 ) ) );
				} );
		} );

		var table = EwfTable.Create(
			fields: new EwfTableField( size: 18.ToEm() ).Append( new EwfTableField( size: 12.ToEm() ) ).Append( new EwfTableField() ).Materialize(),
			headItems: EwfTableItem.Create( "Date/time".ToCell().Append( "Level".ToCell() ).Append( "Details".ToCell() ).Materialize() ).ToCollection(),
			defaultItemLimit: DataRowLimit.Fifty );

		table.AddData(
			events,
			logEvent => {
				var detailsExpandedPmv = new PageModificationValue<string>();
				var detailsExpanded = detailsExpandedPmv.ToCondition( bool.TrueString.ToCollection() );
				var detailsExpandedFieldId = new HiddenFieldId();
				return EwfTableItem.Create(
					getTimeCell( logEvent.time )
						.Append( logEvent.level.ToCell() )
						.Append(
							new EwfButton(
									new CustomButtonStyle(
										classes: new ElementClass( "icon" ),
										attributes: new ElementAttribute( "aria-label", "Expand" ).ToCollection(),
										children:
										new FontAwesomeIcon(
											detailsExpandedPmv.ToCondition( bool.FalseString.ToCollection() )
												.ToElementClassSet( new ElementClass( "fa-plus-square" ) )
												.Add( detailsExpanded.ToElementClassSet( new ElementClass( "fa-minus-square" ) ) )
												.Add( new ElementClass( "fa-lg" ) ) ).ToCollection() ),
									behavior: new CustomButtonBehavior( () => detailsExpandedFieldId.GetJsValueModificationStatements(
										"document.getElementById( '{0}' ).value === '{2}' ? '{1}' : '{2}'".FormatWith(
											detailsExpandedFieldId.ElementId.Id,
											bool.FalseString,
											bool.TrueString ) ) ) )
								.Append<FlowComponent>(
									new GenericFlowContainer(
										new DisplayableElement( _ => new DisplayableElementData(
												null,
												() => new DisplayableElementLocalData( "pre" ),
												children: getMessageContent( logEvent.message, detailsExpanded ) ) )
											.Append( getPropertiesComponent( logEvent.properties, detailsExpanded ) )
											.Materialize() ) )
								.Materialize()
								.ToCell(
									setup: new TableCellSetup(
										etherealContent: new EwfHiddenField( bool.FalseString, id: detailsExpandedFieldId, pageModificationValue: detailsExpandedPmv ).PageComponent
											.ToCollection() ) ) )
						.Materialize() );
			} );

		return new UiPageContent( bodyClasses: new ElementClass( "ewfDiagnosticLog" /* This is used by EWF CSS files. */ ) ).Add( table );
	}

	private EwfTableCell getTimeCell( string dbTime ) {
		var time = dbTimePattern.Parse( dbTime ).GetValueOrThrow().InZone( timeZone );
		return ( time.Offset.Equals( currentOffset ) ? timePattern : timePatternWithOffset ).Format( time ).ToCell();
	}

	private IReadOnlyCollection<FlowComponent> getMessageContent( string message, PageModificationValueCondition expanded ) {
		var newline = Environment.NewLine;
		var newlineIndex = message.IndexOf( newline, StringComparison.Ordinal );
		if( newlineIndex == -1 )
			return message.ToComponents( disableNewlineReplacement: true );

		var firstLine = message[ ..newlineIndex ];
		var remaining = message[ ( newlineIndex + newline.Length ).. ];
		return firstLine.ToComponents( disableNewlineReplacement: true )
			.Append<FlowComponent>( new GenericFlowContainer( remaining.ToComponents( disableNewlineReplacement: true ), displaySetup: expanded.ToDisplaySetup() ) )
			.Materialize();
	}

	private FlowComponent getPropertiesComponent( string properties, PageModificationValueCondition expanded ) =>
		new EwfFigure(
			new DisplayableElement( _ => new DisplayableElementData(
				null,
				() => new DisplayableElementLocalData( "pre" ),
				children: JsonNode.Parse( properties )!.ToJsonString( options: new JsonSerializerOptions { WriteIndented = true } )
					.ToComponents( disableNewlineReplacement: true ) ) ).ToCollection(),
			displaySetup: expanded.ToDisplaySetup(),
			caption: new FigureCaption( "Properties".ToComponents(), figureIsTextual: true ) );
}