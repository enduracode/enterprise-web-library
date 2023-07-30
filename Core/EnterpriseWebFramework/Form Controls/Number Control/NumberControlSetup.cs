#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Humanizer;
using Tewl.InputValidation;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The configuration for a number control.
	/// </summary>
	public class NumberControlSetup {
		/// <summary>
		/// Creates a setup object for a standard number control.
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
		public static NumberControlSetup Create(
			DisplaySetup displaySetup = null, ElementClassSet classes = null, string placeholder = "", string autoFillTokens = "",
			SpecifiedValue<FormAction> action = null, FormAction valueChangedAction = null, PageModificationValue<decimal?> pageModificationValue = null,
			Func<bool, bool> validationPredicate = null, Action validationErrorNotifier = null ) {
			return new NumberControlSetup(
				displaySetup,
				false,
				false,
				TextControlSetup.ElementClass.Add( classes ?? ElementClassSet.Empty ),
				placeholder,
				autoFillTokens,
				null,
				action,
				null,
				valueChangedAction,
				pageModificationValue ?? new PageModificationValue<decimal?>(),
				validationPredicate,
				validationErrorNotifier );
		}

		/// <summary>
		/// Creates a setup object for a number control with auto-complete behavior.
		/// </summary>
		/// <param name="autoCompleteResource">The resource containing the auto-complete items. Do not pass null.</param>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the control.</param>
		/// <param name="placeholder">The hint word or phrase that will appear when the control has an empty value. Do not pass null.</param>
		/// <param name="autoFillTokens">A list of auto-fill detail tokens (see
		/// https://html.spec.whatwg.org/multipage/form-control-infrastructure.html#autofill-detail-tokens), or "off" to instruct the browser to disable auto-fill
		/// (see https://stackoverflow.com/a/23234498/35349 for an explanation of why this could be ignored). Do not pass null.</param>
		/// <param name="action">The action that will occur when the user hits Enter on the control. Pass null to use the current default action.</param>
		/// <param name="triggersActionWhenItemSelected">Pass true to also trigger the action when the user selects an auto-complete item.</param>
		/// <param name="valueChangedAction">The action that will occur when the value is changed. Pass null for no action.</param>
		/// <param name="pageModificationValue"></param>
		/// <param name="validationPredicate"></param>
		/// <param name="validationErrorNotifier"></param>
		public static NumberControlSetup CreateAutoComplete(
			ResourceInfo autoCompleteResource, DisplaySetup displaySetup = null, ElementClassSet classes = null, string placeholder = "", string autoFillTokens = "",
			SpecifiedValue<FormAction> action = null, bool triggersActionWhenItemSelected = false, FormAction valueChangedAction = null,
			PageModificationValue<decimal?> pageModificationValue = null, Func<bool, bool> validationPredicate = null, Action validationErrorNotifier = null ) {
			return new NumberControlSetup(
				displaySetup,
				false,
				false,
				TextControlSetup.ElementClass.Add( classes ?? ElementClassSet.Empty ),
				placeholder,
				autoFillTokens,
				autoCompleteResource,
				action,
				triggersActionWhenItemSelected,
				valueChangedAction,
				pageModificationValue ?? new PageModificationValue<decimal?>(),
				validationPredicate,
				validationErrorNotifier );
		}

		/// <summary>
		/// Creates a setup object for a read-only number control.
		/// </summary>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the control.</param>
		/// <param name="validationPredicate"></param>
		/// <param name="validationErrorNotifier"></param>
		public static NumberControlSetup CreateReadOnly(
			DisplaySetup displaySetup = null, ElementClassSet classes = null, Func<bool, bool> validationPredicate = null, Action validationErrorNotifier = null ) {
			return new NumberControlSetup(
				displaySetup,
				false,
				true,
				TextControlSetup.ElementClass.Add( classes ?? ElementClassSet.Empty ),
				"",
				"",
				null,
				null,
				null,
				null,
				new PageModificationValue<decimal?>(),
				validationPredicate,
				validationErrorNotifier );
		}

		internal readonly Func<decimal?, bool, decimal?, decimal?, decimal?, Action<decimal?, Validator>, ( FormControlLabeler, PhrasingComponent, EwfValidation )>
			LabelerAndComponentAndValidationGetter;

		internal NumberControlSetup(
			DisplaySetup displaySetup, bool isImprecise, bool isReadOnly, ElementClassSet classes, string placeholder, string autoFillTokens,
			ResourceInfo autoCompleteResource, SpecifiedValue<FormAction> specifiedAction, bool? triggersActionWhenItemSelected, FormAction valueChangedAction,
			object pageModificationValue, Func<bool, bool> validationPredicate, Action validationErrorNotifier ) {
			var labeler = new FormControlLabeler();
			var action = specifiedAction != null ? specifiedAction.Value : FormState.Current.FormControlDefaultAction;

			LabelerAndComponentAndValidationGetter = ( value, allowEmpty, minValue, maxValue, valueStep, validationMethod ) => {
				var id = new ElementId();
				var formValue = new FormValue<decimal?>(
					() => value,
					() => isReadOnly ? "" : id.Id,
					v => v.ToString(),
					rawValue => rawValue == null ? PostBackValueValidationResult<decimal?>.CreateInvalid() :
					            !rawValue.Any() && !isImprecise ? PostBackValueValidationResult<decimal?>.CreateValid( null ) :
					            !decimal.TryParse(
						            rawValue,
						            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent,
						            CultureInfo.InvariantCulture,
						            out var result ) ? PostBackValueValidationResult<decimal?>.CreateInvalid() :
					            PostBackValueValidationResult<decimal?>.CreateValid( result ) );

				if( isImprecise )
					formValue.AddPageModificationValue( (PageModificationValue<decimal>)pageModificationValue, v => v.Value );
				else
					formValue.AddPageModificationValue( (PageModificationValue<decimal?>)pageModificationValue, v => v );

				return ( labeler, new CustomPhrasingComponent(
						       new DisplayableElement(
							       context => {
								       if( !isReadOnly ) {
									       action?.AddToPageIfNecessary();
									       valueChangedAction?.AddToPageIfNecessary();
								       }

								       return new DisplayableElementData(
									       displaySetup,
									       () => {
										       var attributes = new List<ElementAttribute>();
										       attributes.Add( new ElementAttribute( "type", isImprecise ? "range" : "number" ) );
										       if( !isReadOnly )
											       attributes.Add( new ElementAttribute( "name", context.Id ) );

										       var pmvValue = isImprecise
											                      ? ( (PageModificationValue<decimal>)pageModificationValue ).Value
											                      : ( (PageModificationValue<decimal?>)pageModificationValue ).Value;
										       attributes.Add( new ElementAttribute( "value", getHtmlFloatingPointNumber( pmvValue ) ) );
										       if( isReadOnly )
											       attributes.Add( new ElementAttribute( "disabled" ) );
										       if( !isReadOnly ) {
											       if( minValue.HasValue )
												       attributes.Add( new ElementAttribute( "min", getHtmlFloatingPointNumber( minValue.Value ) ) );
											       if( maxValue.HasValue )
												       attributes.Add( new ElementAttribute( "max", getHtmlFloatingPointNumber( maxValue.Value ) ) );
											       attributes.Add( new ElementAttribute( "step", valueStep.HasValue ? getHtmlFloatingPointNumber( valueStep.Value ) : "any" ) );
										       }
										       if( placeholder.Any() )
											       attributes.Add( new ElementAttribute( "placeholder", placeholder ) );
										       if( autoFillTokens.Any() )
											       attributes.Add( new ElementAttribute( "autocomplete", autoFillTokens ) );


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
											       !isReadOnly
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
											       ( isImprecise
												         ? ( (PageModificationValue<decimal>)pageModificationValue ).GetJsModificationStatements( "Number( $( this ).val() )" )
												         : ( (PageModificationValue<decimal?>)pageModificationValue ).GetJsModificationStatements( "Number( $( this ).val() )" ) )
											       .Surround( "$( '#{0}' ).change( function() {{ ".FormatWith( context.Id ), " } );" ),
											       autoCompleteStatements );

										       return new DisplayableElementLocalData(
											       "input",
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
									       classes: classes,
									       clientSideIdReferences: id.ToCollection().Append( labeler.ControlId ) );
							       },
							       formValue: formValue ).ToCollection() ), validationMethod == null
								                                                ? null
								                                                : formValue.CreateValidation(
									                                                ( postBackValue, validator ) => {
										                                                if( validationPredicate != null && !validationPredicate( postBackValue.ChangedOnPostBack ) )
											                                                return;

										                                                if( postBackValue.Value.HasValue ) {
											                                                if( minValue.HasValue && postBackValue.Value.Value < minValue.Value ) {
												                                                validator.NoteErrorAndAddMessage( "The value is too small." );
												                                                validationErrorNotifier?.Invoke();
												                                                return;
											                                                }
											                                                if( maxValue.HasValue && postBackValue.Value.Value > maxValue.Value ) {
												                                                validator.NoteErrorAndAddMessage( "The value is too large." );
												                                                validationErrorNotifier?.Invoke();
												                                                return;
											                                                }
											                                                if( valueStep.HasValue && ( postBackValue.Value.Value - ( minValue ?? value ?? 0 ) ) %
											                                                    valueStep.Value != 0 ) {
												                                                validator.NoteErrorAndAddMessage( "The value is not an allowed step." );
												                                                validationErrorNotifier?.Invoke();
												                                                return;
											                                                }
										                                                }
										                                                else if( !allowEmpty ) {
											                                                validator.NoteErrorAndAddMessage( "Please enter a value." );
											                                                validationErrorNotifier?.Invoke();
											                                                return;
										                                                }

										                                                validationMethod( postBackValue.Value, validator );
									                                                } ) );
			};
		}

		private string getHtmlFloatingPointNumber( decimal? value ) => value.HasValue ? value.Value.ToString( "G", CultureInfo.InvariantCulture ) : "";
	}
}