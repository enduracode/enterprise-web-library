#nullable disable
using JetBrains.Annotations;
using Tewl.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A two-state control (i.e. checkbox).
/// </summary>
public class Checkbox: FormControl<PhrasingComponent> {
	private static readonly ElementClass elementClass = new( "ewfCb" );

	[ UsedImplicitly ]
	private class CssElementCreator: ControlCssElementCreator {
		IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() =>
			new CssElement( "Checkbox", "label.{0}".FormatWith( elementClass.ClassName ) ).ToCollection()
				.Append( new CssElement( "CheckboxControl", "input.{0}".FormatWith( elementClass.ClassName ) ) )
				.Append( new CssElement( "CheckboxLabel", "span.{0}".FormatWith( elementClass.ClassName ) ) )
				.Materialize();
	}

	public PhrasingComponent PageComponent { get; }
	public EwfValidation Validation { get; }

	/// <summary>
	/// Creates a checkbox.
	/// </summary>
	/// <param name="value"></param>
	/// <param name="label">The checkbox label. Do not pass null. Pass an empty collection for no label.</param>
	/// <param name="setup">The setup object for the checkbox.</param>
	/// <param name="validationMethod">The validation method. Pass null if you’re only using this control for page modification.</param>
	public Checkbox(
		bool value, IReadOnlyCollection<PhrasingComponent> label, CheckboxSetup setup = null, Action<PostBackValue<bool>, Validator> validationMethod = null ) {
		setup ??= CheckboxSetup.Create();
		var pageModificationValue = setup.PageModificationValue ?? new PageModificationValue<bool>();

		var id = new ElementId();
		var formValue = new FormValue<bool>(
			() => value,
			() => setup.IsReadOnly ? "" : id.Id,
			v => v.ToString(),
			rawValue => rawValue == null ? PostBackValueValidationResult<bool>.CreateValid( false ) :
			            rawValue == "on" ? PostBackValueValidationResult<bool>.CreateValid( true ) : PostBackValueValidationResult<bool>.CreateInvalid() );

		PageComponent = getComponent(
			formValue,
			id,
			null,
			setup.DisplaySetup,
			setup.IsReadOnly,
			setup.Classes,
			pageModificationValue,
			label,
			setup.Action,
			setup.ValueChangedAction,
			() => ( setup.ValueChangedAction?.GetJsStatements() ?? "" ).ConcatenateWithSpace( pageModificationValue.GetJsModificationStatements( "this.checked" ) ) );

		formValue.AddPageModificationValue( pageModificationValue, v => v );

		if( validationMethod != null )
			Validation = formValue.CreateValidation( validationMethod );
	}

	/// <summary>
	/// Creates a radio button.
	/// </summary>
	internal Checkbox(
		FormValue<ElementId> formValue, ElementId id, RadioButtonSetup setup, IReadOnlyCollection<PhrasingComponent> label, FormAction selectionChangedAction,
		Func<string> jsClickStatementGetter, EwfValidation validation, string listItemId = null ) {
		PageComponent = getComponent(
			formValue,
			id,
			listItemId,
			setup.DisplaySetup,
			setup.IsReadOnly,
			setup.Classes,
			setup.PageModificationValue,
			label,
			setup.Action,
			selectionChangedAction,
			() => setup.IsReadOnly
				      ? ""
				      : ( setup.PageModificationValue.Value ? "" : selectionChangedAction?.GetJsStatements() ?? "" ).ConcatenateWithSpace( jsClickStatementGetter() ) );
		Validation = validation;
	}

	private PhrasingComponent getComponent(
		FormValue formValue, ElementId id, string radioButtonListItemId, DisplaySetup displaySetup, bool isReadOnly, ElementClassSet classes,
		PageModificationValue<bool> pageModificationValue, IReadOnlyCollection<PhrasingComponent> label, FormAction action, FormAction valueChangedAction,
		Func<string> jsClickStatementGetter ) {
		return new CustomPhrasingComponent(
			new DisplayableElement(
				labelContext => new DisplayableElementData(
					displaySetup,
					() => new DisplayableElementLocalData( "label" ),
					classes: elementClass.Add( classes ?? ElementClassSet.Empty ),
					children: new DisplayableElement(
							context => {
								if( !isReadOnly ) {
									action?.AddToPageIfNecessary();
									valueChangedAction?.AddToPageIfNecessary();
								}

								return new DisplayableElementData(
									null,
									() => {
										var attributes = new List<ElementAttribute>();
										var radioButtonFormValue = formValue as FormValue<ElementId>;
										attributes.Add( new ElementAttribute( "type", radioButtonFormValue != null ? "radio" : "checkbox" ) );
										if( radioButtonFormValue != null || !isReadOnly )
											attributes.Add(
												new ElementAttribute( "name", radioButtonFormValue != null ? ( (FormValue)radioButtonFormValue ).GetPostBackValueKey() : context.Id ) );
										if( radioButtonFormValue != null )
											attributes.Add( new ElementAttribute( "value", radioButtonListItemId ?? context.Id ) );
										if( pageModificationValue.Value )
											attributes.Add( new ElementAttribute( "checked" ) );
										if( isReadOnly )
											attributes.Add( new ElementAttribute( "disabled" ) );

										var jsInitStatements = StringTools.ConcatenateWithDelimiter(
											" ",
											!isReadOnly
												? SubmitButton.GetImplicitSubmissionKeyPressStatements( action, false )
													.Surround( "$( '#{0}' ).keypress( function( e ) {{ ".FormatWith( context.Id ), " } );" )
												: "",
											jsClickStatementGetter().Surround( "$( '#{0}' ).click( function() {{ ".FormatWith( context.Id ), " } );" ) );

										return new DisplayableElementLocalData(
											"input",
											new FocusabilityCondition( !isReadOnly ),
											isFocused => {
												if( isFocused )
													attributes.Add( new ElementAttribute( "autofocus" ) );
												return new DisplayableElementFocusDependentData( attributes: attributes, includeIdAttribute: true, jsInitStatements: jsInitStatements );
											} );
									},
									classes: elementClass,
									clientSideIdReferences: id.ToCollection() );
							},
							formValue: formValue ).ToCollection()
						.Concat( label.Any() ? new GenericPhrasingContainer( label, classes: elementClass ).ToCollection() : Enumerable.Empty<FlowComponent>() )
						.Materialize() ) ).ToCollection() );
	}

	FormControlLabeler FormControl<PhrasingComponent>.Labeler => null;
}