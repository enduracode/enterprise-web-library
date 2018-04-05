using System;
using System.Collections.Generic;
using System.Linq;
using EnterpriseWebLibrary.InputValidation;
using Humanizer;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The configuration for a text control.
	/// </summary>
	public class TextControlSetup {
		[ UsedImplicitly ]
		private class CssElementCreator: ControlCssElementCreator {
			IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() {
				return new[]
					{
						new CssElement( "SingleLineTextControlAllStates", getSingleLineSelectors( new[] { ":enabled:not(:focus)", ":enabled:focus", ":disabled" } ).ToArray() ),
						new CssElement( "SingleLineTextControlNormalState", getSingleLineSelectors( ":enabled:not(:focus)".ToCollection() ).ToArray() ),
						new CssElement( "SingleLineTextControlFocusState", getSingleLineSelectors( ":enabled:focus".ToCollection() ).ToArray() ),
						new CssElement( "SingleLineTextControlReadOnlyState", getSingleLineSelectors( ":disabled".ToCollection() ).ToArray() ),
						new CssElement(
							"MultilineTextControlAllStates",
							new[] { ":enabled:not(:focus)", ":enabled:focus", ":disabled" }.Select( getMultilineSelector ).ToArray() ),
						new CssElement( "MultilineTextControlNormalState", getMultilineSelector( ":enabled:not(:focus)" ) ),
						new CssElement( "MultilineTextControlFocusState", getMultilineSelector( ":enabled:focus" ) ),
						new CssElement( "MultilineTextControlReadOnlyState", getMultilineSelector( ":disabled" ) )
					};
			}

			private IEnumerable<string> getSingleLineSelectors( IEnumerable<string> suffixes ) =>
				from control in new[] { "input[type=\"text\"]", "input[type=\"password\"]" } from suffix in suffixes select control + suffix;

			private string getMultilineSelector( string suffix ) => "textarea" + suffix;
		}

		/// <summary>
		/// Creates a setup object for a standard text control.
		/// </summary>
		/// <param name="displaySetup"></param>
		/// <param name="numberOfRows">The number of lines in the text control. Must be one or more.</param>
		/// <param name="classes">The classes on the control.</param>
		/// <param name="placeholder">The hint word or phrase that will appear when the control has an empty value. Do not pass null.</param>
		/// <param name="autoFillTokens">A list of auto-fill detail tokens (see
		/// https://html.spec.whatwg.org/multipage/form-control-infrastructure.html#autofill-detail-tokens), or "off" to instruct the browser to disable auto-fill
		/// (see https://stackoverflow.com/a/23234498/35349 for an explanation of why this could be ignored). Do not pass null.</param>
		/// <param name="checksSpellingAndGrammar">Pass true to enable spelling and grammar checking, false to disable it, and null for default behavior.</param>
		/// <param name="action">The action that will occur when the user hits Enter on the control. Pass null to use the current default action. Currently has no
		/// effect for multiline controls.</param>
		/// <param name="valueChangedAction">The action that will occur when the value is changed. Pass null for no action.</param>
		/// <param name="pageModificationValue"></param>
		/// <param name="validationPredicate"></param>
		/// <param name="validationErrorNotifier"></param>
		public static TextControlSetup Create(
			DisplaySetup displaySetup = null, int numberOfRows = 1, ElementClassSet classes = null, string placeholder = "", string autoFillTokens = "",
			bool? checksSpellingAndGrammar = null, FormAction action = null, FormAction valueChangedAction = null,
			PageModificationValue<string> pageModificationValue = null, Func<bool, bool> validationPredicate = null, Action validationErrorNotifier = null ) {
			return new TextControlSetup(
				displaySetup,
				numberOfRows,
				false,
				classes,
				placeholder,
				autoFillTokens,
				null,
				checksSpellingAndGrammar,
				action,
				null,
				valueChangedAction,
				pageModificationValue,
				validationPredicate,
				validationErrorNotifier );
		}

		/// <summary>
		/// Creates a setup object for a text control with auto-complete behavior.
		/// </summary>
		/// <param name="autoCompleteResource">The resource containing the auto-complete items. Do not pass null.</param>
		/// <param name="displaySetup"></param>
		/// <param name="numberOfRows">The number of lines in the text control. Must be one or more.</param>
		/// <param name="classes">The classes on the control.</param>
		/// <param name="placeholder">The hint word or phrase that will appear when the control has an empty value. Do not pass null.</param>
		/// <param name="autoFillTokens">A list of auto-fill detail tokens (see
		/// https://html.spec.whatwg.org/multipage/form-control-infrastructure.html#autofill-detail-tokens), or "off" to instruct the browser to disable auto-fill
		/// (see https://stackoverflow.com/a/23234498/35349 for an explanation of why this could be ignored). Do not pass null.</param>
		/// <param name="checksSpellingAndGrammar">Pass true to enable spelling and grammar checking, false to disable it, and null for default behavior.</param>
		/// <param name="action">The action that will occur when the user hits Enter on the control. Pass null to use the current default action. Currently has no
		/// effect for multiline controls.</param>
		/// <param name="triggersActionWhenItemSelected">Pass true to also trigger the action when the user selects an auto-complete item.</param>
		/// <param name="valueChangedAction">The action that will occur when the value is changed. Pass null for no action.</param>
		/// <param name="pageModificationValue"></param>
		/// <param name="validationPredicate"></param>
		/// <param name="validationErrorNotifier"></param>
		public static TextControlSetup CreateAutoComplete(
			ResourceInfo autoCompleteResource, DisplaySetup displaySetup = null, int numberOfRows = 1, ElementClassSet classes = null, string placeholder = "",
			string autoFillTokens = "", bool? checksSpellingAndGrammar = null, FormAction action = null, bool triggersActionWhenItemSelected = false,
			FormAction valueChangedAction = null, PageModificationValue<string> pageModificationValue = null, Func<bool, bool> validationPredicate = null,
			Action validationErrorNotifier = null ) {
			return new TextControlSetup(
				displaySetup,
				numberOfRows,
				false,
				classes,
				placeholder,
				autoFillTokens,
				autoCompleteResource,
				checksSpellingAndGrammar,
				action,
				triggersActionWhenItemSelected,
				valueChangedAction,
				pageModificationValue,
				validationPredicate,
				validationErrorNotifier );
		}

		/// <summary>
		/// Creates a setup object for a read-only text control.
		/// </summary>
		/// <param name="displaySetup"></param>
		/// <param name="numberOfRows">The number of lines in the text control. Must be one or more.</param>
		/// <param name="classes">The classes on the control.</param>
		/// <param name="validationPredicate"></param>
		/// <param name="validationErrorNotifier"></param>
		public static TextControlSetup CreateReadOnly(
			DisplaySetup displaySetup = null, int numberOfRows = 1, ElementClassSet classes = null, Func<bool, bool> validationPredicate = null,
			Action validationErrorNotifier = null ) {
			return new TextControlSetup(
				displaySetup,
				numberOfRows,
				true,
				classes,
				"",
				"",
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
			DisplaySetup displaySetup = null, ElementClassSet classes = null, string placeholder = "", string autoFillTokens = "", FormAction action = null,
			FormAction valueChangedAction = null, PageModificationValue<string> pageModificationValue = null, Func<bool, bool> validationPredicate = null,
			Action validationErrorNotifier = null ) {
			return new TextControlSetup(
				displaySetup,
				null,
				false,
				classes,
				placeholder,
				autoFillTokens,
				null,
				null,
				action,
				null,
				valueChangedAction,
				pageModificationValue,
				validationPredicate,
				validationErrorNotifier );
		}

		internal readonly Func<string, bool, int?, Action<string, Validator>, (PhrasingComponent, EwfValidation)> ComponentAndValidationGetter;

		private TextControlSetup(
			DisplaySetup displaySetup, int? numberOfRows, bool isReadOnly, ElementClassSet classes, string placeholder, string autoFillTokens,
			ResourceInfo autoCompleteResource, bool? checksSpellingAndGrammar, FormAction action, bool? triggersActionWhenItemSelected, FormAction valueChangedAction,
			PageModificationValue<string> pageModificationValue, Func<bool, bool> validationPredicate, Action validationErrorNotifier ) {
			action = action ?? FormState.Current.DefaultAction;
			pageModificationValue = pageModificationValue ?? new PageModificationValue<string>();

			ComponentAndValidationGetter = ( value, allowEmpty, maxLength, validationMethod ) => {
				var id = new ElementId();
				var formValue = new FormValue<string>(
					() => value,
					() => isReadOnly ? "" : id.Id,
					v => v,
					rawValue => rawValue != null ? PostBackValueValidationResult<string>.CreateValid( rawValue ) : PostBackValueValidationResult<string>.CreateInvalid() );

				formValue.AddPageModificationValue( pageModificationValue, v => v );

				return ( new CustomPhrasingComponent(
					new DisplayableElement(
						context => {
							id.AddId( context.Id );

							if( !isReadOnly ) {
								if( !numberOfRows.HasValue || numberOfRows.Value == 1 || ( autoCompleteResource != null && triggersActionWhenItemSelected.Value ) )
									action.AddToPageIfNecessary();
								valueChangedAction?.AddToPageIfNecessary();
							}

							return new DisplayableElementData(
								displaySetup,
								() => {
									var attributes = new List<Tuple<string, string>>();

									if( !numberOfRows.HasValue )
										attributes.Add( Tuple.Create( "type", "password" ) );
									else if( numberOfRows.Value == 1 )
										attributes.Add( Tuple.Create( "type", "text" ) );

									if( !isReadOnly )
										attributes.Add( Tuple.Create( "name", context.Id ) );

									if( !numberOfRows.HasValue || numberOfRows.Value == 1 )
										attributes.Add( Tuple.Create( "size", ( maxLength ?? 1000 ).ToString() ) );
									else
										attributes.Add( Tuple.Create( "rows", numberOfRows.ToString() ) );

									if( !numberOfRows.HasValue || numberOfRows.Value == 1 )
										attributes.Add( Tuple.Create( "value", numberOfRows.HasValue ? pageModificationValue.Value : "" ) );
									if( isReadOnly )
										attributes.Add( Tuple.Create( "disabled", "disabled" ) );
									if( !isReadOnly && maxLength.HasValue )
										attributes.Add( Tuple.Create( "maxlength", maxLength.Value.ToString() ) );
									if( placeholder.Any() )
										attributes.Add( Tuple.Create( "placeholder", placeholder ) );
									if( autoFillTokens.Any() )
										attributes.Add( Tuple.Create( "autocomplete", autoFillTokens ) );
									if( !isReadOnly )
										attributes.Add( Tuple.Create( "inputmode", "text" ) );
									if( checksSpellingAndGrammar.HasValue )
										attributes.Add( Tuple.Create( "spellcheck", checksSpellingAndGrammar.Value ? "true" : "false" ) );


									var autoCompleteStatements = "";
									if( autoCompleteResource != null ) {
										const int delay = 250; // Default delay is 300 ms.
										const int minCharacters = 3;

										var autocompleteOptions = new List<Tuple<string, string>>();
										autocompleteOptions.Add( Tuple.Create( "delay", delay.ToString() ) );
										autocompleteOptions.Add( Tuple.Create( "minLength", minCharacters.ToString() ) );
										autocompleteOptions.Add( Tuple.Create( "source", "'" + autoCompleteResource.GetUrl() + "'" ) );

										if( triggersActionWhenItemSelected.Value ) {
											var handler = "function( event, ui ) {{ $( '#{0}' ).val( ui.item.value ); {1} return false; }}".FormatWith( context.Id, action.GetJsStatements() );
											autocompleteOptions.Add( Tuple.Create( "select", handler ) );
										}

										autoCompleteStatements = "$( '#{0}' ).autocomplete( {{ {1} }} );".FormatWith(
											context.Id,
											autocompleteOptions.Select( o => "{0}: {1}".FormatWith( o.Item1, o.Item2 ) ).GetCommaDelimitedStringFromCollection() );
									}

									var jsInitStatements = StringTools.ConcatenateWithDelimiter(
										" ",
										( !numberOfRows.HasValue || numberOfRows.Value == 1 ) && !isReadOnly
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
										pageModificationValue.GetJsModificationStatements( "$( this ).val()" )
											.Surround( "$( '#{0}' ).change( function() {{ ".FormatWith( context.Id ), " } );" ),
										autoCompleteStatements );

									return new DisplayableElementLocalData(
										!numberOfRows.HasValue || numberOfRows.Value == 1 ? "input" : "textarea",
										new FocusabilityCondition( true ),
										isFocused => {
											if( isFocused )
												attributes.Add( Tuple.Create( "autofocus", "autofocus" ) );
											return new DisplayableElementFocusDependentData(
												attributes: attributes,
												includeIdAttribute: jsInitStatements.Any(),
												jsInitStatements: jsInitStatements );
										} );
								},
								classes: classes,
								children: !numberOfRows.HasValue || numberOfRows.Value == 1
									          ? null
									          : new TextNode( () => Controls.EwfTextBox.GetTextareaValue( pageModificationValue.Value ) ).ToCollection() );
						},
						formValue: formValue ).ToCollection() ), formValue.CreateValidation(
					( postBackValue, validator ) => {
						if( validationPredicate != null && !validationPredicate( postBackValue.ChangedOnPostBack ) )
							return;

						var errorHandler = new ValidationErrorHandler( "text" );
						var validatedValue = maxLength.HasValue
							                     ? validator.GetString( errorHandler, postBackValue.Value, allowEmpty, maxLength.Value )
							                     : validator.GetString( errorHandler, postBackValue.Value, allowEmpty );
						if( errorHandler.LastResult != ErrorCondition.NoError ) {
							validationErrorNotifier();
							return;
						}

						validationMethod( validatedValue, validator );
					} ) );
			};
		}
	}
}