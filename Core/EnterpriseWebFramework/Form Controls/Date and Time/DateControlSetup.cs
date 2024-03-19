using JetBrains.Annotations;
using NodaTime;
using NodaTime.Text;
using Tewl.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// The configuration for a date control.
/// </summary>
public class DateControlSetup {
	private static readonly ElementClass elementClass = new( "ewfDc" );

	[ UsedImplicitly ]
	private class CssElementCreator: ControlCssElementCreator {
		IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() =>
			new CssElement( "DateControl", "duet-date-picker.{0}".FormatWith( elementClass.ClassName ) ).ToCollection();
	}

	/// <summary>
	/// Creates a setup object for a standard date control.
	/// </summary>
	/// <param name="displaySetup"></param>
	/// <param name="classes">The classes on the control.</param>
	/// <param name="autoFillTokens">A list of auto-fill detail tokens (see
	/// https://html.spec.whatwg.org/multipage/form-control-infrastructure.html#autofill-detail-tokens), or "off" to instruct the browser to disable auto-fill
	/// (see https://stackoverflow.com/a/23234498/35349 for an explanation of why this could be ignored). Do not pass null.</param>
	/// <param name="action">The action that will occur when the user hits Enter on the control. Pass null to use the current default action.</param>
	/// <param name="valueChangedAction">The action that will occur when the value is changed. Pass null for no action.</param>
	/// <param name="pageModificationValue"></param>
	/// <param name="validationPredicate"></param>
	/// <param name="validationErrorNotifier"></param>
	public static DateControlSetup Create(
		DisplaySetup? displaySetup = null, ElementClassSet? classes = null, string autoFillTokens = "", SpecifiedValue<FormAction>? action = null,
		FormAction? valueChangedAction = null, PageModificationValue<LocalDate?>? pageModificationValue = null, Func<bool, bool>? validationPredicate = null,
		Action? validationErrorNotifier = null ) =>
		new( displaySetup, false, classes, autoFillTokens, action, valueChangedAction, pageModificationValue, validationPredicate, validationErrorNotifier );

	/// <summary>
	/// Creates a setup object for a read-only date control.
	/// </summary>
	/// <param name="displaySetup"></param>
	/// <param name="classes">The classes on the control.</param>
	/// <param name="validationPredicate"></param>
	/// <param name="validationErrorNotifier"></param>
	public static DateControlSetup CreateReadOnly(
		DisplaySetup? displaySetup = null, ElementClassSet? classes = null, Func<bool, bool>? validationPredicate = null,
		Action? validationErrorNotifier = null ) =>
		new( displaySetup, true, classes, "", null, null, null, validationPredicate, validationErrorNotifier );

	internal readonly Func<LocalDate?, bool, LocalDate?, LocalDate?, Action<LocalDate?, Validator>?, ( FormControlLabeler, PhrasingComponent, EwfValidation )>
		LabelerAndComponentAndValidationGetter;

	internal DateControlSetup(
		DisplaySetup? displaySetup, bool isReadOnly, ElementClassSet? classes, string autoFillTokens, SpecifiedValue<FormAction>? specifiedAction,
		FormAction? valueChangedAction, PageModificationValue<LocalDate?>? datePageModificationValue, Func<bool, bool>? validationPredicate,
		Action? validationErrorNotifier ) {
		var labeler = new FormControlLabeler();
		if( autoFillTokens.Length > 0 )
			throw new NotSupportedException( "Auto-fill detail tokens are not supported with the current implementation of the date control." );
		var action = specifiedAction != null ? specifiedAction.Value : FormState.Current.FormControlDefaultAction;
		datePageModificationValue ??= new PageModificationValue<LocalDate?>();

		LabelerAndComponentAndValidationGetter = ( value, allowEmpty, minValue, maxValue, validationMethod ) => {
			var currentDate = PageBase.Current.FirstRequestTime.InUtc().Date;
			minValue ??= currentDate.PlusYears( -120 );
			maxValue ??= currentDate.PlusYears( 5 );

			var id = new ElementId();
			const int textControlMaxLength = 50;
			var formValue = new FormValue<string>(
				() => value.HasValue ? LocalDatePattern.Create( "M/d/yyyy", Cultures.EnglishUnitedStates ).Format( value.Value ) : "",
				() => isReadOnly ? "" : id.Id,
				v => v,
				rawValue => rawValue is null ? PostBackValueValidationResult<string>.CreateInvalid() :
				            rawValue.Length > textControlMaxLength ? PostBackValueValidationResult<string>.CreateInvalid() :
				            PostBackValueValidationResult<string>.CreateValid( rawValue ) );

			var pageModificationValue = new PageModificationValue<string>();
			formValue.AddPageModificationValue( pageModificationValue, v => v.Trim() );
			formValue.AddPageModificationValue(
				datePageModificationValue,
				v => {
					var errorHandler = new ValidationErrorHandler( "value" );
					var validatedValue = new Validator().GetNullableDateTime( errorHandler, v, null, true, DateTime.MinValue, DateTime.MaxValue );
					if( errorHandler.LastResult is not ErrorCondition.NoError )
						validatedValue = null;
					return validatedValue.ToNewUnderlyingValue( LocalDate.FromDateTime );
				} );

			return ( labeler, new CustomPhrasingComponent(
					       new DisplayableElement(
						       context => {
							       if( !isReadOnly ) {
								       action?.AddToPageIfNecessary();
								       valueChangedAction?.AddToPageIfNecessary();
							       }

							       var textControlId = context.Id + "__text";
							       labeler.ControlId.AddId( textControlId );
							       return new DisplayableElementData(
								       displaySetup,
								       () => {
									       var attributes = new List<ElementAttribute>();
									       if( isReadOnly )
										       attributes.Add( new ElementAttribute( "disabled" ) );
									       attributes.Add( new ElementAttribute( "first-day-of-week", "0" ) );
									       attributes.Add( new ElementAttribute( "identifier", textControlId ) );
									       attributes.Add( new ElementAttribute( "max", LocalDatePattern.Iso.Format( maxValue.Value ) ) );
									       attributes.Add( new ElementAttribute( "min", LocalDatePattern.Iso.Format( minValue.Value ) ) );

									       // Use the value of the text control instead of the hidden field to enable round-tripping of invalid dates.
									       attributes.Add( new ElementAttribute( "name", "" ) );
									       var textControlNameAndValueAddStatements =
										       "textControl.name = '{0}'; textControl.value = '{1}';".FormatWith( context.Id, pageModificationValue.Value );

									       attributes.Add(
										       new ElementAttribute(
											       "value",
											       datePageModificationValue.Value.HasValue ? LocalDatePattern.Iso.Format( datePageModificationValue.Value.Value ) : "" ) );

									       return new DisplayableElementLocalData(
										       "duet-date-picker",
										       new FocusabilityCondition( !isReadOnly ),
										       isFocused => new DisplayableElementFocusDependentData(
											       attributes: attributes,
											       includeIdAttribute: true,
											       jsInitStatements: "customElements.whenDefined( 'duet-date-picker' ).then( () => {{ {0} }} );".FormatWith(
												       StringTools.ConcatenateWithDelimiter(
													       " ",
													       "const picker = document.querySelector( '#{0}' );".FormatWith( context.Id ),
													       @"
picker.dateAdapter = {
	parse( value = '', createDate ) {
		const match = value.match( /^(\d{1,2})\/(\d{1,2})\/(\d{4})$/ ); if( match ) return createDate( match[3], match[1], match[2] );
	},
	format( date ) {
		return `${date.getMonth() + 1}/${date.getDate()}/${date.getFullYear()}`;
	}
};
",
													       @"
picker.localization = {{
	buttonLabel: 'Choose date',
	placeholder: 'm/d/y',
	selectedDateMessage: 'Selected date is',
	prevMonthLabel: 'Previous month',
	nextMonthLabel: 'Next month',
	monthSelectLabel: 'Month',
	yearSelectLabel: 'Year',
	closeLabel: 'Close window',
	calendarHeading: 'Choose a date',
	dayNames: [ 'Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday' ],
	monthNames: [ 'January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December' ],
	monthNamesShort: [ 'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec' ],
	locale: '{0}'
}};
".FormatWith( Cultures.EnglishUnitedStates.Name ),
													       "picker.componentOnReady().then( () => {{ const textControl = document.querySelector( '#{0}' ); {1} }} );".FormatWith(
														       textControlId,
														       textControlNameAndValueAddStatements + ( isReadOnly
															                                                ? ""
															                                                : SubmitButton.GetImplicitSubmissionKeyPressStatements( action, false )
																                                                .Surround( "$( textControl ).keypress( function( e ) { ", " } );" ) )
														       .PrependDelimiter( " " ) ),
													       "$( picker ).on( 'duetChange', function( e ) {{ {0} }} );".FormatWith(
														       StringTools.ConcatenateWithDelimiter(
															       " ",
															       valueChangedAction is null ? "" : valueChangedAction.GetJsStatements(),
															       datePageModificationValue.GetJsModificationStatements( "e.originalEvent.detail.value" ) ) ),
													       isFocused ? "picker.setFocus();" : "" ) ) ) );
								       },
								       classes: elementClass.Add( classes ?? ElementClassSet.Empty ),
								       clientSideIdReferences: id.ToCollection() );
						       },
						       formValue: formValue ).ToCollection() ), validationMethod == null
							                                                ? null
							                                                : formValue.CreateValidation(
								                                                ( postBackValue, validator ) => {
									                                                if( validationPredicate != null && !validationPredicate( postBackValue.ChangedOnPostBack ) )
										                                                return;

									                                                var errorHandler = new ValidationErrorHandler( "date" );
									                                                var validatedValue = validator.GetNullableDateTime(
										                                                errorHandler,
										                                                postBackValue.Value.Trim(),
										                                                null,
										                                                allowEmpty,
										                                                minValue.Value.ToDateTimeUnspecified(),
										                                                maxValue.Value.PlusDays( 1 ).ToDateTimeUnspecified() );
									                                                if( errorHandler.LastResult != ErrorCondition.NoError ) {
										                                                validationErrorNotifier?.Invoke();
										                                                return;
									                                                }

									                                                if( validatedValue.HasTime() ) {
										                                                validator.NoteErrorAndAddMessage( "Time information is not allowed." );
										                                                validationErrorNotifier?.Invoke();
										                                                return;
									                                                }

									                                                validationMethod( validatedValue.ToNewUnderlyingValue( LocalDate.FromDateTime ), validator );
								                                                } ) );
		};
	}
}