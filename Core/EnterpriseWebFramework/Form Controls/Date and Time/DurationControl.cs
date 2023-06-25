using System.Text.RegularExpressions;
using JetBrains.Annotations;
using NodaTime;
using Tewl.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A duration edit control.
/// </summary>
public class DurationControl: FormControl<PhrasingComponent> {
	private static readonly ElementClass elementClass = new( "ewfDcc" );

	[ UsedImplicitly ]
	private class CssElementCreator: ControlCssElementCreator {
		IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() =>
			new CssElement( "DurationControlContainer", "span.{0}".FormatWith( elementClass.ClassName ) ).ToCollection();
	}

	public FormControlLabeler Labeler { get; }
	public PhrasingComponent PageComponent { get; }
	public EwfValidation Validation { get; }

	/// <summary>
	/// Creates a duration control.
	/// </summary>
	/// <param name="value"></param>
	/// <param name="allowEmpty"></param>
	/// <param name="setup">The setup object for the duration control.</param>
	/// <param name="validationMethod">The validation method. Pass null if you’re only using this control for page modification.</param>
	public DurationControl( Duration? value, bool allowEmpty, DurationControlSetup setup = null, Action<Duration?, Validator> validationMethod = null ) {
		setup ??= DurationControlSetup.Create();

		var textControl = new TextControl(
			value.HasValue ? Math.Floor( value.Value.TotalHours ).ToString( "0000" ) + ":" + value.Value.Minutes.ToString( "00" ) : "",
			allowEmpty,
			setup: setup.IsReadOnly
				       ? TextControlSetup.CreateReadOnly( validationPredicate: setup.ValidationPredicate, validationErrorNotifier: setup.ValidationErrorNotifier )
				       : TextControlSetup.Create(
					       placeholder: "h:m",
					       action: new SpecifiedValue<FormAction>( setup.Action ),
					       valueChangedAction: setup.ValueChangedAction,
					       pageModificationValue: setup.PageModificationValue,
					       validationPredicate: setup.ValidationPredicate,
					       validationErrorNotifier: setup.ValidationErrorNotifier ),
			validationMethod: validationMethod == null
				                  ? null
				                  : ( postBackValue, validator ) => {
					                  if( tooLongOrInvalidCharacters( postBackValue ) ) {
						                  validator.NoteErrorAndAddMessage( "Please enter a valid duration." );
						                  setup.ValidationErrorNotifier?.Invoke();
						                  return;
					                  }

					                  var errorHandler = new ValidationErrorHandler( "duration" );
					                  var validatedValue = validator.GetNullableTimeSpan( errorHandler, parseTimeSpan( postBackValue ), allowEmpty );
					                  if( errorHandler.LastResult is not ErrorCondition.NoError ) {
						                  setup.ValidationErrorNotifier?.Invoke();
						                  return;
					                  }

					                  validationMethod( validatedValue.ToNewUnderlyingValue( Duration.FromTimeSpan ), validator );
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
							jsInitStatements:
							"{0}.blur( function() {{ {1} }} ).keypress( function( e ) {{ {2} }} ).focus( function() {{ {3} }} ).mouseup( function( e ) {{ {4} }} );"
								.FormatWith(
									getTextControlExpression( context.Id ),
									"ApplyTimeSpanFormat( this );",
									"if( !NumericalOnly( e, this ) ) e.preventDefault();",
									"this.value = this.value.replace( ':', '' ); this.select();",
									"e.preventDefault();" ) ) ),
					classes: elementClass.Add( setup.Classes ?? ElementClassSet.Empty ),
					children: textControl.PageComponent.ToCollection() ) ).ToCollection() );

		Validation = textControl.Validation;
	}

	private bool tooLongOrInvalidCharacters( string value ) {
		const int maxValueLength = 6; // also defined in JavaScript
		return value.Length > ( value.Contains( ":" ) ? maxValueLength + 1 : maxValueLength ) || !value.Equals( Regex.Replace( value, "[^0-9:]", "" ) );
	}

	/// <summary>
	/// Supports browsers with Javascript disabled.
	/// </summary>
	private TimeSpan? parseTimeSpan( string value ) {
		if( !value.Any() )
			return null;

		if( value.Contains( ":" ) ) {
			var splitPartsArray = value.Split( ':' );
			return new TimeSpan( int.Parse( splitPartsArray[ 0 ] ), int.Parse( splitPartsArray[ 1 ] ), 0 );
		}
		// This section supports browsers without script, which we typically don't actually support. However, this code already
		// supported no Javascript, and now despite no evidence to support it, I fear something may be relying on this behavior.
		var intValue = int.Parse( value );
		var hours = (int)( intValue * .01 );
		var minutes = intValue % 100;
		if( minutes > 59 )
			minutes = 59;
		return new TimeSpan( hours, minutes, 0 );
	}

	private string getTextControlExpression( string containerId ) => "$( '#{0}' ).children( 'input' )".FormatWith( containerId );
}