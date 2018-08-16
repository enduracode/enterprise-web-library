using System;
using System.Collections.Generic;
using System.Linq;
using EnterpriseWebLibrary.InputValidation;
using Humanizer;
using JetBrains.Annotations;
using NodaTime;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A date-and-time edit control.
	/// </summary>
	public class DateAndTimeControl: FormControl<PhrasingComponent> {
		private static readonly ElementClass elementClass = new ElementClass( "ewfDtc" );

		[ UsedImplicitly ]
		private class CssElementCreator: ControlCssElementCreator {
			IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() =>
				new CssElement( "DateAndTimeControl", "span.{0}".FormatWith( elementClass.ClassName ) ).ToCollection();
		}

		public FormControlLabeler Labeler { get; }
		public PhrasingComponent PageComponent { get; }
		public EwfValidation Validation { get; }

		/// <summary>
		/// Creates a date control.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="allowEmpty"></param>
		/// <param name="setup">The setup object for the date-and-time control.</param>
		/// <param name="minValue">The earliest acceptable date.</param>
		/// <param name="maxValue">The latest acceptable date.</param>
		/// <param name="validationMethod">The validation method. Pass null if you’re only using this control for page modification.</param>
		public DateAndTimeControl(
			LocalDateTime? value, bool allowEmpty, DateAndTimeControlSetup setup = null, LocalDate? minValue = null, LocalDate? maxValue = null,
			Action<LocalDateTime?, Validator> validationMethod = null ) {
			setup = setup ?? DateAndTimeControlSetup.Create();

			var textControl = new TextControl(
				value.HasValue ? value.Value.ToDateTimeUnspecified().ToMonthDayYearString() + " " + value.Value.ToDateTimeUnspecified().ToHourAndMinuteString() : "",
				allowEmpty,
				setup: setup.IsReadOnly
					       ? TextControlSetup.CreateReadOnly( validationPredicate: setup.ValidationPredicate, validationErrorNotifier: setup.ValidationErrorNotifier )
					       : TextControlSetup.Create(
						       autoFillTokens: setup.AutoFillTokens,
						       action: setup.Action,
						       valueChangedAction: setup.ValueChangedAction,
						       pageModificationValue: setup.PageModificationValue,
						       validationPredicate: setup.ValidationPredicate,
						       validationErrorNotifier: setup.ValidationErrorNotifier ),
				validationMethod: validationMethod == null
					                  ? (Action<string, Validator>)null
					                  : ( postBackValue, validator ) => {
						                  var errorHandler = new ValidationErrorHandler( "date and time" );
						                  var validatedValue = validator.GetNullableDateTime(
							                  errorHandler,
							                  postBackValue.ToUpper(),
							                  DateTimeTools.MonthDayYearFormats.Select( i => i + " " + DateTimeTools.HourAndMinuteFormat ).ToArray(),
							                  allowEmpty,
							                  minValue?.ToDateTimeUnspecified() ?? DateTime.MinValue,
							                  maxValue?.ToDateTimeUnspecified() ?? DateTime.MaxValue );
						                  if( errorHandler.LastResult != ErrorCondition.NoError ) {
							                  setup.ValidationErrorNotifier?.Invoke();
							                  return;
						                  }

						                  validationMethod(
							                  validatedValue.HasValue ? (LocalDateTime?)LocalDateTime.FromDateTime( validatedValue.Value ) : null,
							                  validator );
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
								jsInitStatements: "{0}.datetimepicker( {{ {1} }} );".FormatWith(
									getTextControlExpression( context.Id ),
									StringTools.ConcatenateWithDelimiter(
										", ",
										minValue.HasValue
											? "minDate: {0}".FormatWith( "new Date( {0}, {1} - 1, {2} )".FormatWith( minValue.Value.Year, minValue.Value.Month, minValue.Value.Day ) )
											: "",
										maxValue.HasValue
											? "maxDate: {0}".FormatWith( "new Date( {0}, {1} - 1, {2} )".FormatWith( maxValue.Value.Year, maxValue.Value.Month, maxValue.Value.Day ) )
											: "",
										"timeFormat: 'h:mmt'",
										"stepMinute: {0}".FormatWith( setup.MinuteInterval.Value ) ) ) ) ),
						classes: elementClass.Add( setup.Classes ?? ElementClassSet.Empty ),
						children: textControl.PageComponent.ToCollection()
							.Concat(
								setup.IsReadOnly
									? Enumerable.Empty<PhrasingComponent>()
									: new EwfButton(
										new CustomButtonStyle(
											children: new GenericPhrasingContainer(
												new FontAwesomeIcon( "fa-calendar-o", "fa-stack-2x" ).ToCollection()
													.Append( new FontAwesomeIcon( "fa-clock-o", "fa-stack-1x" ) )
													.Materialize(),
												classes: new ElementClass( "fa-stack" ) ).ToCollection() ),
										behavior: new CustomButtonBehavior( () => "{0}.datetimepicker( 'show' );".FormatWith( getTextControlExpression( context.Id ) ) ),
										classes: new ElementClass( "icon" ) ).ToCollection() )
							.Materialize() ) ).ToCollection() );

			Validation = textControl.Validation;
		}

		private string getTextControlExpression( string containerId ) => "$( '#{0}' ).children( 'input' )".FormatWith( containerId );
	}
}