using JetBrains.Annotations;
using NodaTime;
using Tewl.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A time edit control.
/// </summary>
public class TimeControl: FormControl<FlowComponent> {
	private static readonly ElementClass elementClass = new( "ewfTc" );

	[ UsedImplicitly ]
	private class CssElementCreator: ControlCssElementCreator {
		IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() =>
			new CssElement( "TimeControl", "div.{0}".FormatWith( elementClass.ClassName ) ).ToCollection();
	}

	public FormControlLabeler Labeler { get; }
	public FlowComponent PageComponent { get; }
	public EwfValidation Validation { get; }

	/// <summary>
	/// Creates a time control.
	/// </summary>
	/// <param name="value"></param>
	/// <param name="allowEmpty"></param>
	/// <param name="setup">The setup object for the time control.</param>
	/// <param name="minValue">The earliest allowed time.</param>
	/// <param name="maxValue">The latest allowed time. This can be earlier than <paramref name="minValue"/> to create a range spanning midnight.</param>
	/// <param name="minuteInterval">Allows the user to select values only in the given increments. Be aware that other values can still be sent from the
	/// browser via a crafted request.</param>
	/// <param name="validationMethod">The validation method. Pass null if you’re only using this control for page modification.</param>
	public TimeControl(
		LocalTime? value, bool allowEmpty, TimeControlSetup setup = null, LocalTime? minValue = null, LocalTime? maxValue = null, int minuteInterval = 15,
		Action<LocalTime?, Validator> validationMethod = null ) {
		setup ??= TimeControlSetup.Create();
		minValue ??= LocalTime.Midnight;

		if( minuteInterval < 30 ) {
			var textControl = new TextControl(
				value.HasValue ? new TimeSpan( value.Value.TickOfDay ).ToTimeOfDayHourAndMinuteString() : "",
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
						                  var errorHandler = new ValidationErrorHandler( "time" );
						                  var validatedValue = validator.GetNullableTimeOfDayTimeSpan(
								                  errorHandler,
								                  postBackValue.ToUpper(),
								                  TewlContrib.DateTimeTools.HourAndMinuteFormat.ToCollection().ToArray(),
								                  allowEmpty )
							                  .ToNewUnderlyingValue( v => LocalTime.FromTicksSinceMidnight( v.Ticks ) );
						                  if( errorHandler.LastResult != ErrorCondition.NoError ) {
							                  setup.ValidationErrorNotifier?.Invoke();
							                  return;
						                  }

						                  var wrap = maxValue < minValue.Value;
						                  if( !wrap
							                      ? validatedValue < minValue.Value || validatedValue > maxValue
							                      : validatedValue < minValue.Value && validatedValue > maxValue ) {
							                  validator.NoteErrorAndAddMessage( "The time is too early or too late." );
							                  setup.ValidationErrorNotifier?.Invoke();
							                  return;
						                  }

						                  validationMethod( validatedValue, validator );
					                  } );

			Labeler = textControl.Labeler;

			PageComponent = new DisplayableElement(
				context => new DisplayableElementData(
					setup.DisplaySetup,
					() => new DisplayableElementLocalData(
						"div",
						focusDependentData: new DisplayableElementFocusDependentData(
							includeIdAttribute: true,
							jsInitStatements: "{0}.timepicker( {{ {1} }} );".FormatWith(
								getTextControlExpression( context.Id ),
								StringTools.ConcatenateWithDelimiter(
									", ",
									"timeFormat: 'h:mmt'",
									"stepMinute: {0}".FormatWith( minuteInterval ),
									"showButtonPanel: false" ) ) ) ),
					classes: elementClass.Add( setup.Classes ?? ElementClassSet.Empty ),
					children: textControl.PageComponent.ToCollection()
						.Concat(
							setup.IsReadOnly
								? Enumerable.Empty<PhrasingComponent>()
								: new EwfButton(
									new CustomButtonStyle(
										attributes: new ElementAttribute( "aria-label", "Time picker" ).ToCollection(),
										children: new FontAwesomeIcon( "fa-clock-o" ).ToCollection() ),
									behavior: new CustomButtonBehavior( () => "{0}.timepicker( 'show' );".FormatWith( getTextControlExpression( context.Id ) ) ),
									classes: new ElementClass( "icon" ) ).ToCollection() )
						.Materialize() ) );

			Validation = textControl.Validation;
		}
		else {
			var items = from time in getTimes( minValue.Value, maxValue, minuteInterval )
			            let timeSpan = new TimeSpan( time.TickOfDay )
			            select SelectListItem.Create<LocalTime?>( time, timeSpan.ToTimeOfDayHourAndMinuteString() );
			var selectList = SelectList.CreateDropDown(
				setup.IsReadOnly
					? DropDownSetup.CreateReadOnly(
						items,
						placeholderText: "",
						validationPredicate: setup.ValidationPredicate,
						validationErrorNotifier: setup.ValidationErrorNotifier )
					: DropDownSetup.Create(
						items,
						placeholderText: "",
						autoFillTokens: setup.AutoFillTokens,
						action: new SpecifiedValue<FormAction>( setup.Action ),
						selectionChangedAction: setup.ValueChangedAction,
						validationPredicate: setup.ValidationPredicate,
						validationErrorNotifier: setup.ValidationErrorNotifier ),
				value,
				placeholderIsValid: allowEmpty,
				validationMethod: validationMethod );

			Labeler = selectList.Labeler;

			PageComponent = new GenericFlowContainer(
				selectList.PageComponent.ToCollection(),
				displaySetup: setup.DisplaySetup,
				classes: elementClass.Add( setup.Classes ?? ElementClassSet.Empty ) );

			Validation = selectList.Validation;
		}
	}

	private string getTextControlExpression( string containerId ) => "$( '#{0}' ).children( 'input' )".FormatWith( containerId );

	private IReadOnlyCollection<LocalTime> getTimes( LocalTime minValue, LocalTime? maxValue, int minuteInterval ) {
		var times = new List<LocalTime>();
		var time = minValue;
		var wrapAllowed = maxValue < minValue;
		while( true ) {
			times.Add( time );
			time = time.PlusMinutes( minuteInterval );

			if( time < times.Last() )
				if( wrapAllowed )
					wrapAllowed = false;
				else
					break;

			if( !wrapAllowed && time > maxValue )
				break;
		}
		return times;
	}
}