using System.Text.Json;
using System.Text.Json.Nodes;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.DataAccess.CommandWriting;
using EnterpriseWebLibrary.DataAccess.CommandWriting.InlineConditionAbstraction;
using EnterpriseWebLibrary.DataAccess.CommandWriting.InlineConditionAbstraction.Conditions;
using EnterpriseWebLibrary.DatabaseSpecification;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;
using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure;
using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.ComponentDisplay;
using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.ElementBase.Classification;
using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.GeneralContentModels.Ethereal;
using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.GeneralContentModels.Flow;
using EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic;
using EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic.AlternativeResourceModes;
using EnterpriseWebLibrary.ExternalFunctionality;
using EnterpriseWebLibrary.TewlContrib;
using Humanizer;
using NodaTime;
using NodaTime.Text;
using Tewl.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Admin;

// EwlPage
// OptionalParameter: eventContains
// OptionalParameter: text propertyName
// OptionalParameter: text propertyValue
partial class DebugLog {
	private static readonly InstantPattern dbTimePattern = InstantPattern.CreateWithInvariantCulture( "uuuu'-'MM'-'dd'T'HH':'mm':'ss;FFFFFFFFF" );

	private static readonly ZonedDateTimePattern timePattern = ZonedDateTimePattern.CreateWithInvariantCulture( "MMM'-'dd HH:mm:ss.fff", null );

	private static readonly ZonedDateTimePattern timePatternWithOffset =
		ZonedDateTimePattern.CreateWithInvariantCulture( "MMM'-'dd HH:mm:ss.fff '(UTC'o<+H>')'", null );

	private JsonArray? propertyValueArray;
	private DateTimeZone timeZone = null!;
	private Offset currentOffset;

	protected override void init() {
		var validator = new Validator();
		propertyValueArray = validatePropertyValue( PropertyValue, validator );
		if( validator.ErrorsOccurred )
			throw new MultiMessageException( validator.ErrorMessages );

		timeZone = DateTimeZoneProviders.Tzdb.GetSystemDefault();
		currentOffset = timeZone.GetUtcOffset( EwfRequest.Current!.RequestTime );
	}

	protected override ResourceParent? createParent() => new DiagnosticLog( Es );

	protected override string getResourceName() => "Two-Day Debug Log";

	protected override AlternativeResourceMode? createAlternativeMode() =>
		ExternalFunctionalityStatics.SqliteFunctionalityEnabled ? null : new DisabledResourceMode( "The SQLite provider must be available." );

	protected internal override bool IsSlow => true;

	protected override PageContent getContent() =>
		new FilterPageContent(
			() => FormItemList.CreateStack( generalSetup: new FormItemListSetup( buttonSetup: new ButtonSetup( "Update results" ) ) )
				.AddItem(
					parametersModification.GetEventContainsFormItem(
						true,
						label: "Event message or any property contains".ToComponents(),
						controlSetup: TextControlSetup.Create( placeholder: "term1 term2 term3" ) ) )
				.AddItem( getPropertyFilterItem() )
				.ToCollection(),
			() => {
				DatabaseInfo dbInfo = new SqliteInfo( "Debug Log", EwfConfigurationStatics.AppConfiguration.DebugLogFilePath, true );
				var command = dbInfo.CreateCommand();
				command.CommandText = "SELECT * FROM Events";

				// simple contains filter
				if( EventContains.Pattern.Length > 0 ) {
					command.CommandText += " WHERE ( ( ";
					( (InlineDbCommandCondition)new LikeCondition( LikeCondition.Behavior.AndedTokens, "Message", EventContains.Pattern ) ).AddToCommand(
						command,
						dbInfo,
						"messageContains" );
					command.CommandText += " ) OR ( ";
					( (InlineDbCommandCondition)new LikeCondition( LikeCondition.Behavior.AndedTokens, "Properties", EventContains.Pattern ) ).AddToCommand(
						command,
						dbInfo,
						"propertiesContains" );
					command.CommandText += " ) )";
				}

				// single property filter
				if( PropertyName.Length > 0 ) {
					command.CommandText += EventContains.Pattern.Length > 0 ? " AND " : " WHERE ";

					var nameParameter = new DbCommandParameter( "propertyName", new DbParameterValue( "$." + PropertyName ) );
					command.CommandText += $"Properties -> {nameParameter.GetNameForCommandText( dbInfo )} ";
					command.Parameters.Add( nameParameter.GetAdoDotNetParameter( dbInfo ) );

					if( propertyValueArray is not null ) {
						var parameters = propertyValueArray.Select( ( value, index ) => new DbCommandParameter(
								$"propertyValue{index}",
								new DbParameterValue( value?.ToJsonStringWithSimpleEscaping() ?? "null" ) ) )
							.Materialize();
						command.CommandText += $"IN( {StringTools.ConcatenateWithDelimiter( ", ", parameters.Select( i => i.GetNameForCommandText( dbInfo ) ) )} )";
						foreach( var i in parameters )
							command.Parameters.Add( i.GetAdoDotNetParameter( dbInfo ) );
					}
					else
						command.CommandText += "NOTNULL";
				}

				command.CommandText += " ORDER BY Id DESC";

				var events = new List<( long id, string time, string level, string message, string properties )>( 1000 );
				var connection = new DatabaseConnection( dbInfo );
				connection.ExecuteWithConnectionOpen( () => {
					connection.ExecuteReaderCommand(
						command,
						reader => {
							while( reader.Read() )
								events.Add( ( get<long>( 0 ), get<string>( 1 ), get<string>( 2 ), get<string>( 3 ), get<string>( 5 ) ) );

							return;
							T get<T>( int ordinal ) => (T)reader.GetValue( ordinal );
						} );
				} );

				var latestEventId = ComponentStateItem.Create( "latestEventId", events.Select( i => (long?)i.id ).FirstOrDefault(), _ => true, false );
				var latestEventIndex = latestEventId.Value.HasValue ? events.FindIndex( i => i.id == latestEventId.Value.Value ) : events.Count;
				var visibleEvents = latestEventIndex == -1 ? events : events[ latestEventIndex.. ];

				var components = new List<FlowComponent>();
				var newEventRegion = new UpdateRegionSet();
				var refilterRegion = new UpdateRegionSet();
				components.Add(
					new FlowIdContainer(
						latestEventIndex < 1
							? [ ]
							: new EwfButton(
									new StandardButtonStyle(
										"Show " + "new event".ToQuantity( latestEventIndex ),
										icon: new ActionComponentIcon( new FontAwesomeIcon( "fa-refresh" ) ) ),
									behavior: new PostBackBehavior(
										postBack: PostBack.CreateIntermediate( newEventRegion.Add( visibleEvents.Any() ? null : refilterRegion ), id: "showNewEvents" ) ) )
								.ToCollection(),
						updateRegionSets: newEventRegion ) );

				components.Add(
					new FlowIdContainer(
						!visibleEvents.Any()
							? [ ]
							: FormState.ExecuteWithActions(
								PostBack.CreateFull( id: "refilter" ),
								() => FormItemList.CreateWrapping( setup: new FormItemListSetup( buttonSetup: new ButtonSetup( "Refilter" ) ) )
									.AddItem(
										parametersModification.GetPropertyNameFormItem(
											false,
											label: [ ],
											controlSetup: TextControlSetup.Create( widthOverride: 15.ToEm(), placeholder: "property name" ),
											value: "",
											additionalValidationMethod: validator => {
												parametersModification.EventContains = new PatternString( "" );

												var values = visibleEvents
													.SelectMany( i =>
														( (JsonObject)JsonNode.Parse( i.properties )! ).TryGetPropertyValue( parametersModification.PropertyName, out var node )
															? ( node?.ToJsonStringWithSimpleEscaping() ?? "null" ).ToCollection()
															: [ ] )
													.Distinct( StringComparer.Ordinal )
													.Materialize();
												if( values.Any() )
													parametersModification.PropertyValue = StringTools.ConcatenateWithDelimiter( ",", values );
												else
													validator.NoteErrorAndAddMessage( "The property does not appear in these results." );
											} ) )
									.ToCollection() ),
						updateRegionSets: refilterRegion ) );

				components.Add(
					EwfTable.Create(
							fields: new EwfTableField( size: 18.ToEm() ).Append( new EwfTableField( size: 12.ToEm() ) ).Append( new EwfTableField() ).Materialize(),
							headItems: EwfTableItem.Create( "Date/time".ToCell().Append( "Level".ToCell() ).Append( "Details".ToCell() ).Materialize() ).ToCollection(),
							defaultItemLimit: DataRowLimit.Fifty,
							tailUpdateRegions: new TailUpdateRegion( newEventRegion, visibleEvents.Count ),
							etherealContent: new EtherealIdContainer( latestEventId.ToCollection(), updateRegionSets: newEventRegion ).ToCollection() )
						.AddData(
							visibleEvents,
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
														etherealContent: new EwfHiddenField( bool.FalseString, id: detailsExpandedFieldId, pageModificationValue: detailsExpandedPmv )
															.PageComponent.ToCollection() ) ) )
										.Materialize() );
							} ) );

				return components;
			},
			bodyClasses: new ElementClass( "ewfDiagnosticLog" /* This is used by EWF CSS files. */ ),
			disableInitialResultLoading: true );

	private FormItem getPropertyFilterItem() {
		var name = parametersModification.GetPropertyNameFormItem( true, controlSetup: TextControlSetup.Create( widthOverride: 15.ToEm(), placeholder: "name" ) )
			.ToComponentCollection( omitLabel: true );
		var value = parametersModification.GetPropertyValueFormItem(
				true,
				controlSetup: TextControlSetup.Create( placeholder: """value1, "stringValue2", {"json":"value3"}""" ),
				additionalValidationMethod: validator => validatePropertyValue( parametersModification.PropertyValue, validator ) )
			.ToComponentCollection( omitLabel: true );
		return new GenericFlowContainer(
			name.Append( new GenericPhrasingContainer( "is".ToComponents() ) ).Concat( value ).Materialize(),
			classes: new ElementClass( "propertyFilter" /* This is used by EWF CSS files. */ ) ).ToFormItem(
			label: "Single property".ToComponents(),
			validation: new EwfValidation( validator => {
				if( parametersModification.PropertyValue.Length > 0 && parametersModification.PropertyName.Length == 0 )
					validator.NoteErrorAndAddMessage( "Please enter a property name for the value." );
			} ) );
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
					children: JsonNode.Parse( properties )!.ToJsonStringWithSimpleEscaping( writeIndented: true ).ToComponents( disableNewlineReplacement: true ) ) )
				.ToCollection(),
			displaySetup: expanded.ToDisplaySetup(),
			caption: new FigureCaption( "Properties".ToComponents(), figureIsTextual: true ) );

	private JsonArray? validatePropertyValue( string value, Validator validator ) {
		if( value.Length == 0 )
			return null;

		try {
			return (JsonArray)JsonNode.Parse( $"[{value}]" )!;
		}
		catch( JsonException ) {
			validator.NoteErrorAndAddMessage( "The value(s) must be valid JSON." );
			return null;
		}
	}
}