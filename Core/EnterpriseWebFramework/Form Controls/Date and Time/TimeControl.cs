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
	/// <param name="minValue">The earliest allowed time. Pass null for no minimum, which is the same as passing midnight.</param>
	/// <param name="maxValue">The latest allowed time. This can be earlier than <paramref name="minValue"/> to create a range spanning midnight. Pass null for no
	/// maximum, which is the same as passing <see cref="LocalTime.MaxValue"/>.</param>
	/// <param name="minuteInterval">Allows the user to select values only in the given increments. Be aware that other values can still be sent from the
	/// browser via a crafted request.</param>
	/// <param name="validationMethod">The validation method. Pass null if you’re only using this control for page modification.</param>
	public TimeControl(
		LocalTime? value, bool allowEmpty, TimeControlSetup? setup = null, LocalTime? minValue = null, LocalTime? maxValue = null, int minuteInterval = 15,
		Action<LocalTime?, Validator>? validationMethod = null ) {
		setup ??= TimeControlSetup.Create();

		if( minuteInterval < 30 ) {
			var textControl = new TextControl(
				value.HasValue ? new TimeSpan( value.Value.TickOfDay ).ToTimeOfDayHourAndMinuteString() : "",
				allowEmpty,
				setup: setup.IsReadOnly
					       ? TextControlSetup.CreateReadOnly( validationPredicate: setup.ValidationPredicate, validationErrorNotifier: setup.ValidationErrorNotifier )
					       : TextControlSetup.Create(
						       autoFillTokens: setup.AutoFillTokens,
						       formattedValueExpressionGetter: valueExpression => "formatTime( {0} )".FormatWith( valueExpression ),
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

						                  if( validatedValue.HasValue && !validatedValue.Value.InRange( minValue, maxValue ) ) {
							                  validator.NoteErrorAndAddMessage( "The time is too early or too late." );
							                  setup.ValidationErrorNotifier?.Invoke();
							                  return;
						                  }

						                  validationMethod( validatedValue, validator );
					                  } );

			Labeler = textControl.Labeler;
			var helpBoxId = new ModalBoxId();
			PageComponent = getContainer(
				setup,
				textControl,
				setup.IsReadOnly
					? Enumerable.Empty<PhrasingComponent>()
					: new EwfButton(
							new CustomButtonStyle(
								classes: new ElementClass( "icon" ),
								attributes: new ElementAttribute( "aria-label", "Help" ).ToCollection(),
								children: new FontAwesomeIcon( "fa-question-circle-o" ).ToCollection() ),
							behavior: new OpenModalBehavior(
								helpBoxId,
								etherealChildren: new ModalBox(
									helpBoxId,
									true,
									"Examples:".ToComponents().Append( new LineBreak() ).Concat( "9 am, 2:30p, 1600".ToComponents() ).Materialize() ).ToCollection() ) )
						.ToCollection() );
			Validation = textControl.Validation;
		}
		else {
			var items = from time in LocalTimeTools.GetStepsInRange( minValue, maxValue, minuteInterval )
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
			PageComponent = getContainer( setup, selectList, Enumerable.Empty<PhrasingComponent>() );
			Validation = selectList.Validation;
		}
	}

	private FlowComponent getContainer( TimeControlSetup setup, FormControl<FlowComponent> control, IEnumerable<PhrasingComponent> additionalContent ) =>
		new GenericFlowContainer(
			control.PageComponent.Concat( additionalContent ).Materialize(),
			displaySetup: setup.DisplaySetup,
			classes: elementClass.Add( setup.Classes ?? ElementClassSet.Empty ) );
}