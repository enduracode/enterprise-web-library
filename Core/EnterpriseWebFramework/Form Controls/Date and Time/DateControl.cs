using JetBrains.Annotations;
using NodaTime;
using Tewl.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A date edit control.
/// </summary>
public class DateControl: FormControl<PhrasingComponent> {
	private static readonly ElementClass elementClass = new( "ewfDc" );

	[ UsedImplicitly ]
	private class CssElementCreator: ControlCssElementCreator {
		IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() =>
			new CssElement( "DateControl", "span.{0}".FormatWith( elementClass.ClassName ) ).ToCollection();
	}

	public FormControlLabeler Labeler { get; }
	public PhrasingComponent PageComponent { get; }
	public EwfValidation Validation { get; }

	/// <summary>
	/// Creates a date control.
	/// </summary>
	/// <param name="value"></param>
	/// <param name="allowEmpty"></param>
	/// <param name="setup">The setup object for the date control.</param>
	/// <param name="minValue">The earliest acceptable date.</param>
	/// <param name="maxValue">The latest acceptable date.</param>
	/// <param name="validationMethod">The validation method. Pass null if you’re only using this control for page modification.</param>
	public DateControl(
		LocalDate? value, bool allowEmpty, DateControlSetup setup = null, LocalDate? minValue = null, LocalDate? maxValue = null,
		Action<LocalDate?, Validator> validationMethod = null ) {
		setup ??= DateControlSetup.Create();

		var textControl = new TextControl(
			value.HasValue ? value.Value.ToDateTimeUnspecified().ToMonthDayYearString() : "",
			allowEmpty,
			setup: setup.IsReadOnly
				       ? TextControlSetup.CreateReadOnly( validationPredicate: setup.ValidationPredicate, validationErrorNotifier: setup.ValidationErrorNotifier )
				       : TextControlSetup.Create(
					       autoFillTokens: setup.AutoFillTokens,
					       action: new SpecifiedValue<FormAction>( setup.Action ),
					       valueChangedAction: setup.ValueChangedAction,
					       pageModificationValue: setup.PageModificationValue,
					       validationPredicate: setup.ValidationPredicate,
					       validationErrorNotifier: setup.ValidationErrorNotifier ),
			validationMethod: validationMethod == null
				                  ? null
				                  : ( postBackValue, validator ) => {
					                  var errorHandler = new ValidationErrorHandler( "date" );
					                  var validatedValue = validator.GetNullableDateTime(
						                  errorHandler,
						                  postBackValue,
						                  null,
						                  allowEmpty,
						                  minValue?.ToDateTimeUnspecified() ?? DateTime.MinValue,
						                  maxValue?.PlusDays( 1 ).ToDateTimeUnspecified() ?? DateTime.MaxValue );
					                  if( errorHandler.LastResult != ErrorCondition.NoError ) {
						                  setup.ValidationErrorNotifier?.Invoke();
						                  return;
					                  }

					                  if( validatedValue.HasTime() ) {
						                  validator.NoteErrorAndAddMessage( "Time information is not allowed." );
						                  setup.ValidationErrorNotifier?.Invoke();
						                  return;
					                  }

					                  validationMethod( validatedValue.HasValue ? LocalDate.FromDateTime( validatedValue.Value ) : null, validator );
				                  } );

		Labeler = textControl.Labeler;

		PageComponent = new CustomPhrasingComponent(
			new DisplayableElement(
				context => new DisplayableElementData(
					setup.DisplaySetup,
					() => new DisplayableElementLocalData(
						"span",
						focusDependentData: new DisplayableElementFocusDependentData(
							includeIdAttribute: true,
							jsInitStatements: "{0}.datepicker( {{ {1} }} );".FormatWith(
								getTextControlExpression( context.Id ),
								StringTools.ConcatenateWithDelimiter(
									", ",
									minValue.HasValue
										? "minDate: {0}".FormatWith( "new Date( {0}, {1} - 1, {2} )".FormatWith( minValue.Value.Year, minValue.Value.Month, minValue.Value.Day ) )
										: "",
									maxValue.HasValue
										? "maxDate: {0}".FormatWith( "new Date( {0}, {1} - 1, {2} )".FormatWith( maxValue.Value.Year, maxValue.Value.Month, maxValue.Value.Day ) )
										: "" ) ) ) ),
					classes: elementClass.Add( setup.Classes ?? ElementClassSet.Empty ),
					children: textControl.PageComponent.ToCollection()
						.Concat(
							setup.IsReadOnly
								? Enumerable.Empty<PhrasingComponent>()
								: new EwfButton(
									new CustomButtonStyle(
										attributes: new ElementAttribute( "aria-label", "Date picker" ).ToCollection(),
										children: new FontAwesomeIcon( "fa-calendar" ).ToCollection() ),
									behavior: new CustomButtonBehavior( () => "{0}.datepicker( 'show' );".FormatWith( getTextControlExpression( context.Id ) ) ),
									classes: new ElementClass( "icon" ) ).ToCollection() )
						.Materialize() ) ).ToCollection() );

		Validation = textControl.Validation;
	}

	private string getTextControlExpression( string containerId ) => "$( '#{0}' ).children( 'input' )".FormatWith( containerId );
}