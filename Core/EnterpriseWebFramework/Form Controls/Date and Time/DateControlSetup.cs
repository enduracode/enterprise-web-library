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
			new CssElement( "DateControl", "span.{0}".FormatWith( elementClass.ClassName ) ).ToCollection();
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
		DisplaySetup displaySetup = null, ElementClassSet classes = null, string autoFillTokens = "", SpecifiedValue<FormAction> action = null,
		FormAction valueChangedAction = null, PageModificationValue<string> pageModificationValue = null, Func<bool, bool> validationPredicate = null,
		Action validationErrorNotifier = null ) {
		return new DateControlSetup(
			displaySetup,
			false,
			classes,
			autoFillTokens,
			action,
			valueChangedAction,
			pageModificationValue,
			validationPredicate,
			validationErrorNotifier );
	}

	/// <summary>
	/// Creates a setup object for a read-only date control.
	/// </summary>
	/// <param name="displaySetup"></param>
	/// <param name="classes">The classes on the control.</param>
	/// <param name="validationPredicate"></param>
	/// <param name="validationErrorNotifier"></param>
	public static DateControlSetup CreateReadOnly(
		DisplaySetup displaySetup = null, ElementClassSet classes = null, Func<bool, bool> validationPredicate = null, Action validationErrorNotifier = null ) {
		return new DateControlSetup( displaySetup, true, classes, "", null, null, null, validationPredicate, validationErrorNotifier );
	}

	internal readonly Func<LocalDate?, bool, LocalDate?, LocalDate?, Action<LocalDate?, Validator>, ( FormControlLabeler, PhrasingComponent, EwfValidation )>
		LabelerAndComponentAndValidationGetter;

	internal DateControlSetup(
		DisplaySetup displaySetup, bool isReadOnly, ElementClassSet classes, string autoFillTokens, SpecifiedValue<FormAction> specifiedAction,
		FormAction valueChangedAction, PageModificationValue<string> pageModificationValue, Func<bool, bool> validationPredicate, Action validationErrorNotifier ) {
		var labeler = new FormControlLabeler();
		if( autoFillTokens.Length > 0 )
			throw new NotSupportedException( "Auto-fill detail tokens are not supported with the current implementation of the date control." );
		var action = specifiedAction != null ? specifiedAction.Value : FormState.Current.FormControlDefaultAction;
		pageModificationValue ??= new PageModificationValue<string>();

		LabelerAndComponentAndValidationGetter = ( value, allowEmpty, minValue, maxValue, validationMethod ) => {
			var id = new ElementId();
			const int textControlMaxLength = 50;
			var formValue = new FormValue<string>(
				() => value.HasValue ? LocalDatePattern.Iso.Format( value.Value ) : "",
				() => id.Id,
				v => v,
				rawValue => rawValue is null ? PostBackValueValidationResult<string>.CreateInvalid() :
				            rawValue.Length > textControlMaxLength ? PostBackValueValidationResult<string>.CreateInvalid() :
				            PostBackValueValidationResult<string>.CreateValid( rawValue ) );

			formValue.AddPageModificationValue( pageModificationValue, v => v.Trim() );

			return ( labeler, new CustomPhrasingComponent(
					       new DisplayableElement(
						       context => {
							       if( !isReadOnly ) {
								       action?.AddToPageIfNecessary();
								       valueChangedAction?.AddToPageIfNecessary();
							       }

							       var textControlId = context.Id + "text";
							       labeler.ControlId.AddId( textControlId );
							       return new DisplayableElementData(
								       displaySetup,
								       () => {
									       var attributes = new List<ElementAttribute>();
									       if( isReadOnly )
										       attributes.Add( new ElementAttribute( "disabled" ) );
									       attributes.Add( new ElementAttribute( "identifier", textControlId ) );
									       if( maxValue.HasValue )
										       attributes.Add( new ElementAttribute( "max", LocalDatePattern.Iso.Format( maxValue.Value ) ) );
									       if( minValue.HasValue )
										       attributes.Add( new ElementAttribute( "min", LocalDatePattern.Iso.Format( minValue.Value ) ) );
									       attributes.Add( new ElementAttribute( "name", context.Id ) );
									       attributes.Add( new ElementAttribute( "value", pageModificationValue.Value ) );

									       // NOTE: Implement additional JS behavior.
									       return new DisplayableElementLocalData(
										       "duet-date-picker",
										       new FocusabilityCondition( !isReadOnly ),
										       isFocused => new DisplayableElementFocusDependentData(
											       attributes: attributes,
											       includeIdAttribute: true,
											       jsInitStatements: StringTools.ConcatenateWithDelimiter(
												       " ",
												       "const picker = document.querySelector( '#{0}' );".FormatWith( context.Id ),
												       isFocused ? "picker.setFocus();" : "" ) ) );
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
										                                                minValue?.ToDateTimeUnspecified() ?? DateTime.MinValue,
										                                                maxValue?.PlusDays( 1 ).ToDateTimeUnspecified() ?? DateTime.MaxValue );
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