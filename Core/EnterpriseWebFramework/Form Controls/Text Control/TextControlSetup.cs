#nullable disable
using System.Globalization;
using JetBrains.Annotations;
using Tewl.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// The configuration for a text control.
/// </summary>
public class TextControlSetup {
	internal static readonly ElementClass ElementClass = new( "ewfTextC" );

	[ UsedImplicitly ]
	private class CssElementCreator: ControlCssElementCreator {
		IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() {
			return new[]
				{
					new CssElement(
						"SingleLineTextControlAllStates",
						new[] { ":enabled:not(:focus)", ":enabled:focus", ":disabled" }.Select( getSingleLineSelector ).ToArray() ),
					new CssElement( "SingleLineTextControlNormalState", getSingleLineSelector( ":enabled:not(:focus)" ) ),
					new CssElement( "SingleLineTextControlFocusState", getSingleLineSelector( ":enabled:focus" ) ),
					new CssElement( "SingleLineTextControlReadOnlyState", getSingleLineSelector( ":disabled" ) ),
					new CssElement(
						"MultilineTextControlAllStates",
						new[] { ":enabled:not(:focus)", ":enabled:focus", ":disabled" }.Select( getMultilineSelector ).ToArray() ),
					new CssElement( "MultilineTextControlNormalState", getMultilineSelector( ":enabled:not(:focus)" ) ),
					new CssElement( "MultilineTextControlFocusState", getMultilineSelector( ":enabled:focus" ) ),
					new CssElement( "MultilineTextControlReadOnlyState", getMultilineSelector( ":disabled" ) )
				};
		}

		private string getSingleLineSelector( string suffix ) => "input.{0}".FormatWith( ElementClass.ClassName ) + suffix;
		private string getMultilineSelector( string suffix ) => "textarea.{0}".FormatWith( ElementClass.ClassName ) + suffix;
	}

	internal static string GetTextareaValue( string value ) {
		// The initial NewLine is here because of http://haacked.com/archive/2008/11/18/new-line-quirk-with-html-textarea.aspx and because this is what Microsoft
		// does in their System.Web.UI.WebControls.TextBox implementation.
		return Environment.NewLine + value;
	}

	/// <summary>
	/// Creates a setup object for a standard text control.
	/// </summary>
	/// <param name="displaySetup"></param>
	/// <param name="widthOverride">The width of the control. This overrides any value that may be specified via CSS. If no width is specified via CSS and you
	/// pass null for this parameter, the width will be based on the maximum number of characters a user can input.</param>
	/// <param name="numberOfRows">The number of lines in the text control. Must be one or more.</param>
	/// <param name="classes">The classes on the control.</param>
	/// <param name="disableTrimming">Pass true to disable white-space trimming.</param>
	/// <param name="placeholder">The hint word or phrase that will appear when the control has an empty value. Do not pass null.</param>
	/// <param name="autoFillTokens">A list of auto-fill detail tokens (see
	/// https://html.spec.whatwg.org/multipage/form-control-infrastructure.html#autofill-detail-tokens), or "off" to instruct the browser to disable auto-fill
	/// (see https://stackoverflow.com/a/23234498/35349 for an explanation of why this could be ignored). Do not pass null.</param>
	/// <param name="checksSpellingAndGrammar">Pass true to enable spelling and grammar checking, false to disable it, and null for default behavior.</param>
	/// <param name="formattedValueExpressionGetter">A function that takes the JavaScript value expression and returns an expression that yields the formatted
	/// value. The expression is executed whenever the control loses focus, including before a post-back, and the control’s current value is replaced with the
	/// formatted value. Do not return null.</param>
	/// <param name="action">The action that will occur when the user hits Enter on the control. Pass null to use the current default action. Currently has no
	/// effect for multiline controls.</param>
	/// <param name="valueChangedAction">The action that will occur when the value is changed. Pass null for no action.</param>
	/// <param name="pageModificationValue"></param>
	/// <param name="validationPredicate"></param>
	/// <param name="validationErrorNotifier"></param>
	public static TextControlSetup Create(
		DisplaySetup displaySetup = null, ContentBasedLength widthOverride = null, int numberOfRows = 1, ElementClassSet classes = null,
		bool disableTrimming = false, string placeholder = "", string autoFillTokens = "", bool? checksSpellingAndGrammar = null,
		Func<string, string> formattedValueExpressionGetter = null, SpecifiedValue<FormAction> action = null, FormAction valueChangedAction = null,
		PageModificationValue<string> pageModificationValue = null, Func<bool, bool> validationPredicate = null, Action validationErrorNotifier = null ) {
		return new TextControlSetup(
			displaySetup,
			numberOfRows == 1 ? "text" : "",
			widthOverride,
			numberOfRows,
			false,
			classes,
			disableTrimming,
			false,
			placeholder,
			autoFillTokens,
			null,
			checksSpellingAndGrammar,
			formattedValueExpressionGetter,
			action,
			null,
			valueChangedAction,
			pageModificationValue,
			null,
			validationPredicate,
			validationErrorNotifier );
	}

	/// <summary>
	/// Creates a setup object for a text control with auto-complete behavior.
	/// </summary>
	/// <param name="autoCompleteResource">The resource containing the auto-complete items. Do not pass null.</param>
	/// <param name="displaySetup"></param>
	/// <param name="widthOverride">The width of the control. This overrides any value that may be specified via CSS. If no width is specified via CSS and you
	/// pass null for this parameter, the width will be based on the maximum number of characters a user can input.</param>
	/// <param name="numberOfRows">The number of lines in the text control. Must be one or more.</param>
	/// <param name="classes">The classes on the control.</param>
	/// <param name="disableTrimming">Pass true to disable white-space trimming.</param>
	/// <param name="placeholder">The hint word or phrase that will appear when the control has an empty value. Do not pass null.</param>
	/// <param name="autoFillTokens">A list of auto-fill detail tokens (see
	/// https://html.spec.whatwg.org/multipage/form-control-infrastructure.html#autofill-detail-tokens), or "off" to instruct the browser to disable auto-fill
	/// (see https://stackoverflow.com/a/23234498/35349 for an explanation of why this could be ignored). Do not pass null.</param>
	/// <param name="checksSpellingAndGrammar">Pass true to enable spelling and grammar checking, false to disable it, and null for default behavior.</param>
	/// <param name="formattedValueExpressionGetter">A function that takes the JavaScript value expression and returns an expression that yields the formatted
	/// value. The expression is executed whenever the control loses focus, including before a post-back, and the control’s current value is replaced with the
	/// formatted value. Do not return null.</param>
	/// <param name="action">The action that will occur when the user hits Enter on the control. Pass null to use the current default action. Currently has no
	/// effect for multiline controls.</param>
	/// <param name="triggersActionWhenItemSelected">Pass true to also trigger the action when the user selects an auto-complete item.</param>
	/// <param name="valueChangedAction">The action that will occur when the value is changed. Pass null for no action.</param>
	/// <param name="pageModificationValue"></param>
	/// <param name="validationPredicate"></param>
	/// <param name="validationErrorNotifier"></param>
	public static TextControlSetup CreateAutoComplete(
		ResourceInfo autoCompleteResource, DisplaySetup displaySetup = null, ContentBasedLength widthOverride = null, int numberOfRows = 1,
		ElementClassSet classes = null, bool disableTrimming = false, string placeholder = "", string autoFillTokens = "", bool? checksSpellingAndGrammar = null,
		Func<string, string> formattedValueExpressionGetter = null, SpecifiedValue<FormAction> action = null, bool triggersActionWhenItemSelected = false,
		FormAction valueChangedAction = null, PageModificationValue<string> pageModificationValue = null, Func<bool, bool> validationPredicate = null,
		Action validationErrorNotifier = null ) {
		return new TextControlSetup(
			displaySetup,
			numberOfRows == 1 ? "text" : "",
			widthOverride,
			numberOfRows,
			false,
			classes,
			disableTrimming,
			false,
			placeholder,
			autoFillTokens,
			autoCompleteResource,
			checksSpellingAndGrammar,
			formattedValueExpressionGetter,
			action,
			triggersActionWhenItemSelected,
			valueChangedAction,
			pageModificationValue,
			null,
			validationPredicate,
			validationErrorNotifier );
	}

	/// <summary>
	/// Creates a setup object for a read-only text control.
	/// </summary>
	/// <param name="displaySetup"></param>
	/// <param name="widthOverride">The width of the control. This overrides any value that may be specified via CSS. If no width is specified via CSS and you
	/// pass null for this parameter, the width will be based on the maximum number of characters a user can input.</param>
	/// <param name="numberOfRows">The number of lines in the text control. Must be one or more.</param>
	/// <param name="classes">The classes on the control.</param>
	/// <param name="validationPredicate"></param>
	/// <param name="validationErrorNotifier"></param>
	public static TextControlSetup CreateReadOnly(
		DisplaySetup displaySetup = null, ContentBasedLength widthOverride = null, int numberOfRows = 1, ElementClassSet classes = null,
		Func<bool, bool> validationPredicate = null, Action validationErrorNotifier = null ) {
		return new TextControlSetup(
			displaySetup,
			numberOfRows == 1 ? "text" : "",
			widthOverride,
			numberOfRows,
			true,
			classes,
			false,
			false,
			"",
			"",
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			validationPredicate,
			validationErrorNotifier );
	}

	/// <summary>
	/// Creates a setup object for an obscured (i.e. password) text control.
	/// </summary>
	/// <param name="displaySetup"></param>
	/// <param name="widthOverride">The width of the control. This overrides any value that may be specified via CSS. If no width is specified via CSS and you
	/// pass null for this parameter, the width will be based on the maximum number of characters a user can input.</param>
	/// <param name="classes">The classes on the control.</param>
	/// <param name="placeholder">The hint word or phrase that will appear when the control has an empty value. Do not pass null.</param>
	/// <param name="autoFillTokens">A list of auto-fill detail tokens (see
	/// https://html.spec.whatwg.org/multipage/form-control-infrastructure.html#autofill-detail-tokens), or "off" to instruct the browser to disable auto-fill
	/// (see https://stackoverflow.com/a/23234498/35349 for an explanation of why this could be ignored). Do not pass null.</param>
	/// <param name="action">The action that will occur when the user hits Enter on the control. Pass null to use the current default action.</param>
	/// <param name="valueChangedAction">The action that will occur when the value is changed. Pass null for no action.</param>
	/// <param name="pageModificationValue"></param>
	/// <param name="validationPredicate"></param>
	/// <param name="validationErrorNotifier"></param>
	/// <returns></returns>
	public static TextControlSetup CreateObscured(
		DisplaySetup displaySetup = null, ContentBasedLength widthOverride = null, ElementClassSet classes = null, string placeholder = "",
		string autoFillTokens = "", SpecifiedValue<FormAction> action = null, FormAction valueChangedAction = null,
		PageModificationValue<string> pageModificationValue = null, Func<bool, bool> validationPredicate = null, Action validationErrorNotifier = null ) {
		return new TextControlSetup(
			displaySetup,
			"password",
			widthOverride,
			null,
			false,
			classes,
			true,
			false,
			placeholder,
			autoFillTokens,
			null,
			null,
			null,
			action,
			null,
			valueChangedAction,
			pageModificationValue,
			null,
			validationPredicate,
			validationErrorNotifier );
	}

	internal readonly
		Func<string, bool, int?, int?, Func<string, Validator, string>, Action<string, Validator>, ( FormControlLabeler, PhrasingComponent, EwfValidation )>
		LabelerAndComponentAndValidationGetter;

	internal TextControlSetup(
		DisplaySetup displaySetup, string inputElementType, ContentBasedLength widthOverride, int? numberOfRows, bool isReadOnly, ElementClassSet classes,
		bool disableTrimming, bool requiresNumericValue, string placeholder, string autoFillTokens, ResourceInfo autoCompleteResource,
		bool? checksSpellingAndGrammar, Func<string, string> formattedValueExpressionGetter, SpecifiedValue<FormAction> specifiedAction,
		bool? triggersActionWhenItemSelected, FormAction valueChangedAction, PageModificationValue<string> pageModificationValueParameter,
		PageModificationValue<long?> numericPageModificationValue, Func<bool, bool> validationPredicate, Action validationErrorNotifier ) {
		formattedValueExpressionGetter ??= _ => "";
		var action = specifiedAction != null ? specifiedAction.Value : FormState.Current.FormControlDefaultAction;

		LabelerAndComponentAndValidationGetter = ( value, allowEmpty, minLength, maxLength, internalValidationMethod, externalValidationMethod ) => {
			var pageModificationValue = pageModificationValueParameter ?? new PageModificationValue<string>();

			var labeler = new FormControlLabeler();

			var id = new ElementId();
			var formValue = new FormValue<string>(
				() => value,
				() => isReadOnly ? "" : id.Id,
				v => v,
				rawValue => rawValue == null ? PostBackValueValidationResult<string>.CreateInvalid() :
				            maxLength.HasValue && rawValue.Length > maxLength.Value ? PostBackValueValidationResult<string>.CreateInvalid() :
				            PostBackValueValidationResult<string>.CreateValid( rawValue ) );

			formValue.AddPageModificationValue( pageModificationValue, v => disableTrimming ? v : v.Trim() );
			if( numericPageModificationValue != null )
				formValue.AddPageModificationValue(
					numericPageModificationValue,
					v => long.TryParse( v, NumberStyles.None, CultureInfo.InvariantCulture, out var result ) ? result : null );

			return ( labeler, new CustomPhrasingComponent(
					       new DisplayableElement(
						       context => {
							       if( !isReadOnly ) {
								       if( inputElementType.Any() || ( autoCompleteResource != null && triggersActionWhenItemSelected.Value ) )
									       action?.AddToPageIfNecessary();
								       valueChangedAction?.AddToPageIfNecessary();
							       }

							       return new DisplayableElementData(
								       displaySetup,
								       () => {
									       var attributes = new List<ElementAttribute>();
									       if( inputElementType.Any() )
										       attributes.Add( new ElementAttribute( "type", inputElementType ) );
									       if( !isReadOnly )
										       attributes.Add( new ElementAttribute( "name", context.Id ) );

									       if( inputElementType.Any() )
										       attributes.Add( new ElementAttribute( "size", ( maxLength is < 1000 ? maxLength : 1000 ).ToString() ) );
									       else
										       attributes.Add( new ElementAttribute( "rows", numberOfRows.Value.ToString() ) );

									       if( inputElementType.Any() )
										       attributes.Add( new ElementAttribute( "value", inputElementType != "password" ? pageModificationValue.Value : "" ) );
									       if( isReadOnly )
										       attributes.Add( new ElementAttribute( "disabled" ) );
									       if( minLength.HasValue )
										       attributes.Add( new ElementAttribute( "minlength", minLength.Value.ToString() ) );
									       if( maxLength.HasValue )
										       attributes.Add( new ElementAttribute( "maxlength", maxLength.Value.ToString() ) );
									       if( requiresNumericValue )
										       attributes.Add( new ElementAttribute( "pattern", "[0-9]*" ) );
									       if( placeholder.Any() )
										       attributes.Add( new ElementAttribute( "placeholder", placeholder ) );
									       if( autoFillTokens.Any() )
										       attributes.Add( new ElementAttribute( "autocomplete", autoFillTokens ) );
									       attributes.Add(
										       new ElementAttribute(
											       "inputmode",
											       inputElementType == "email" ? "email" :
											       inputElementType == "tel" ? "tel" :
											       inputElementType == "url" ? "url" :
											       requiresNumericValue ? "numeric" : "text" ) );
									       if( checksSpellingAndGrammar.HasValue )
										       attributes.Add( new ElementAttribute( "spellcheck", checksSpellingAndGrammar.Value ? "true" : "false" ) );
									       if( widthOverride != null )
										       attributes.Add( new ElementAttribute( "style", "width: {0}".FormatWith( ( (CssLength)widthOverride ).Value ) ) );


									       var autoCompleteStatements = "";
									       if( autoCompleteResource != null ) {
										       const int delay = 250; // Default delay is 300 ms.
										       const int minCharacters = 3;

										       var autocompleteOptions = new List<Tuple<string, string>>();
										       autocompleteOptions.Add( Tuple.Create( "delay", delay.ToString() ) );
										       autocompleteOptions.Add( Tuple.Create( "minLength", minCharacters.ToString() ) );
										       autocompleteOptions.Add( Tuple.Create( "source", "'" + autoCompleteResource.GetUrl() + "'" ) );

										       if( action != null && triggersActionWhenItemSelected.Value ) {
											       var handler = "function( event, ui ) {{ $( '#{0}' ).val( ui.item.value ); {1} return false; }}".FormatWith(
												       context.Id,
												       action.GetJsStatements() );
											       autocompleteOptions.Add( Tuple.Create( "select", handler ) );
										       }

										       autoCompleteStatements = "$( '#{0}' ).autocomplete( {{ {1} }} );".FormatWith(
											       context.Id,
											       autocompleteOptions.Select( o => "{0}: {1}".FormatWith( o.Item1, o.Item2 ) ).GetCommaDelimitedStringFromCollection() );
									       }

									       var jsInitStatements = StringTools.ConcatenateWithDelimiter(
										       " ",
										       inputElementType.Any() && !isReadOnly
											       ? SubmitButton.GetImplicitSubmissionKeyPressStatements( action, valueChangedAction != null )
												       .Surround( "$( '#{0}' ).keypress( function( e ) {{ ".FormatWith( context.Id ), " } );" )
											       : "",
										       valueChangedAction != null
											       ?
											       // Use setTimeout to prevent keypress and change from *both* triggering actions at the same time when Enter is pressed after a text
											       // change.
											       "$( '#{0}' ).change( function() {{ {1} }} );".FormatWith(
												       context.Id,
												       "setTimeout( function() { " + valueChangedAction.GetJsStatements() + " }, 0 );" )
											       : "",
										       formattedValueExpressionGetter( "$( this ).val()" )
											       .Surround( "$( this ).val( ", " );" )
											       .ConcatenateWithSpace(
												       pageModificationValue.GetJsModificationStatements( "$( this ).val()" + ( disableTrimming ? "" : ".trim()" ) ) )
											       .ConcatenateWithSpace(
												       numericPageModificationValue?.GetJsModificationStatements( "Number( {0} )".FormatWith( "$( this ).val()" ) ) ?? "" )
											       .Surround( "$( '#{0}' ).change( function() {{ ".FormatWith( context.Id ), " } );" ),
										       autoCompleteStatements,
										       requiresNumericValue ? "initNumericTextControl( '#{0}' );".FormatWith( context.Id ) : "" );

									       return new DisplayableElementLocalData(
										       inputElementType.Any() ? "input" : "textarea",
										       new FocusabilityCondition( !isReadOnly ),
										       isFocused => {
											       if( isFocused )
												       attributes.Add( new ElementAttribute( "autofocus" ) );
											       return new DisplayableElementFocusDependentData(
												       attributes: attributes,
												       includeIdAttribute: true,
												       jsInitStatements: jsInitStatements );
										       } );
								       },
								       classes: ElementClass.Add( classes ?? ElementClassSet.Empty ),
								       clientSideIdReferences: id.ToCollection().Append( labeler.ControlId ),
								       children: inputElementType.Any() ? null : new TextNode( () => GetTextareaValue( pageModificationValue.Value ) ).ToCollection() );
						       },
						       formValue: formValue ).ToCollection() ), externalValidationMethod == null
							                                                ? null
							                                                : formValue.CreateValidation(
								                                                ( postBackValue, validator ) => {
									                                                if( validationPredicate != null && !validationPredicate( postBackValue.ChangedOnPostBack ) )
										                                                return;

									                                                string validatedValue;
									                                                if( inputElementType != "password"
										                                                    ? postBackValue.Value.Trim().Any()
										                                                    : postBackValue.Value.Any() )
										                                                validatedValue = internalValidationMethod(
											                                                disableTrimming ? postBackValue.Value : postBackValue.Value.Trim(),
											                                                validator );
									                                                else if( allowEmpty )
										                                                validatedValue = "";
									                                                else {
										                                                validatedValue = null;
										                                                validator.NoteErrorAndAddMessage( "Please enter a value." );
									                                                }

									                                                if( validatedValue == null ) {
										                                                validationErrorNotifier?.Invoke();
										                                                return;
									                                                }

									                                                externalValidationMethod( validatedValue, validator );
								                                                } ) );
		};
	}
}