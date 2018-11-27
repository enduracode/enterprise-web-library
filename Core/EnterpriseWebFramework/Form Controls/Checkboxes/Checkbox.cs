using System;
using System.Collections.Generic;
using System.Linq;
using EnterpriseWebLibrary.InputValidation;
using Humanizer;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A two-state control (i.e. checkbox).
	/// </summary>
	public class Checkbox: FormControl<PhrasingComponent> {
		private static readonly ElementClass elementClass = new ElementClass( "ewfCb" );

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
			setup = setup ?? CheckboxSetup.Create();

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
				setup.PageModificationValue,
				label,
				setup.Action,
				setup.ValueChangedAction,
				() => ( setup.ValueChangedAction?.GetJsStatements() ?? "" ).ConcatenateWithSpace(
					setup.PageModificationValue.GetJsModificationStatements( "this.checked" ) ) );

			formValue.AddPageModificationValue( setup.PageModificationValue, v => v );

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
					      : ( setup.PageModificationValue.Value ? "" : selectionChangedAction?.GetJsStatements() ?? "" )
					      .ConcatenateWithSpace( jsClickStatementGetter() ) );
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
									id.AddId( context.Id );

									if( !isReadOnly ) {
										action.AddToPageIfNecessary();
										valueChangedAction?.AddToPageIfNecessary();
									}

									return new DisplayableElementData(
										null,
										() => {
											var attributes = new List<Tuple<string, string>>();
											var radioButtonFormValue = formValue as FormValue<ElementId>;
											attributes.Add( Tuple.Create( "type", radioButtonFormValue != null ? "radio" : "checkbox" ) );
											if( radioButtonFormValue != null || !isReadOnly )
												attributes.Add(
													Tuple.Create( "name", radioButtonFormValue != null ? ( (FormValue)radioButtonFormValue ).GetPostBackValueKey() : context.Id ) );
											if( radioButtonFormValue != null )
												attributes.Add( Tuple.Create( "value", radioButtonListItemId ?? context.Id ) );
											if( pageModificationValue.Value )
												attributes.Add( Tuple.Create( "checked", "checked" ) );
											if( isReadOnly )
												attributes.Add( Tuple.Create( "disabled", "disabled" ) );

											var jsInitStatements = StringTools.ConcatenateWithDelimiter(
												" ",
												!isReadOnly
													? SubmitButton.GetImplicitSubmissionKeyPressStatements( action, false )
														.Surround( "$( '#{0}' ).keypress( function( e ) {{ ".FormatWith( context.Id ), " } );" )
													: "",
												jsClickStatementGetter().Surround( "$( '#{0}' ).click( function() {{ ".FormatWith( context.Id ), " } );" ) );

											return new DisplayableElementLocalData(
												"input",
												new FocusabilityCondition( true ),
												isFocused => {
													if( isFocused )
														attributes.Add( Tuple.Create( "autofocus", "autofocus" ) );
													return new DisplayableElementFocusDependentData(
														attributes: attributes,
														includeIdAttribute: true,
														jsInitStatements: jsInitStatements );
												} );
										},
										classes: elementClass );
								},
								formValue: formValue ).ToCollection()
							.Concat( label.Any() ? new GenericPhrasingContainer( label, classes: elementClass ).ToCollection() : Enumerable.Empty<FlowComponent>() )
							.Materialize() ) ).ToCollection() );
		}

		FormControlLabeler FormControl<PhrasingComponent>.Labeler => null;
	}
}