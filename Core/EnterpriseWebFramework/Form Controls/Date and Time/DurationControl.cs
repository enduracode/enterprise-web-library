#nullable disable
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
					       formattedValueExpressionGetter: valueExpression => "formatDuration( {0} )".FormatWith( valueExpression ),
					       action: new SpecifiedValue<FormAction>( setup.Action ),
					       valueChangedAction: setup.ValueChangedAction,
					       pageModificationValue: setup.PageModificationValue,
					       validationPredicate: setup.ValidationPredicate,
					       validationErrorNotifier: setup.ValidationErrorNotifier ),
			maxLength: 7,
			validationMethod: validationMethod == null
				                  ? null
				                  : ( postBackValue, validator ) => {
					                  Duration? validatedValue = null;
					                  if( postBackValue.Length > 0 ) {
						                  var match = Regex.Match( postBackValue, @"^(?<h>[0-9]{1,4}):(?<m>[0-9]{2})\z" );
						                  if( !match.Success ) {
							                  validator.NoteErrorAndAddMessage( "Please enter a valid duration." );
							                  setup.ValidationErrorNotifier?.Invoke();
							                  return;
						                  }

						                  var hours = int.Parse( match.Groups[ "h" ].Value );
						                  var minutes = int.Parse( match.Groups[ "m" ].Value );
						                  if( minutes > 59 ) {
							                  validator.NoteErrorAndAddMessage( "Please enter a valid duration." );
							                  setup.ValidationErrorNotifier?.Invoke();
							                  return;
						                  }

						                  validatedValue = Duration.FromMinutes( hours * 60 + minutes );
					                  }

					                  validationMethod( validatedValue, validator );
				                  } );

		Labeler = textControl.Labeler;

		PageComponent = new GenericPhrasingContainer(
			textControl.PageComponent.ToCollection(),
			displaySetup: setup.DisplaySetup,
			classes: elementClass.Add( setup.Classes ?? ElementClassSet.Empty ) );

		Validation = textControl.Validation;
	}
}