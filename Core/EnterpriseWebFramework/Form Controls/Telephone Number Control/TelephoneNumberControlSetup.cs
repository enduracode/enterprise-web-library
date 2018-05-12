using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The configuration for a telephone-number control.
	/// </summary>
	public class TelephoneNumberControlSetup {
		/// <summary>
		/// Creates a setup object for a standard telephone-number control.
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
		public static TelephoneNumberControlSetup Create(
			DisplaySetup displaySetup = null, ElementClassSet classes = null, string placeholder = "", string autoFillTokens = "", FormAction action = null,
			FormAction valueChangedAction = null, PageModificationValue<string> pageModificationValue = null, Func<bool, bool> validationPredicate = null,
			Action validationErrorNotifier = null ) {
			return new TelephoneNumberControlSetup(
				new TextControlSetup(
					displaySetup,
					"tel",
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
					validationErrorNotifier ) );
		}

		/// <summary>
		/// Creates a setup object for a telephone-number control with auto-complete behavior.
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
		public static TelephoneNumberControlSetup CreateAutoComplete(
			ResourceInfo autoCompleteResource, DisplaySetup displaySetup = null, ElementClassSet classes = null, string placeholder = "", string autoFillTokens = "",
			FormAction action = null, bool triggersActionWhenItemSelected = false, FormAction valueChangedAction = null,
			PageModificationValue<string> pageModificationValue = null, Func<bool, bool> validationPredicate = null, Action validationErrorNotifier = null ) {
			return new TelephoneNumberControlSetup(
				new TextControlSetup(
					displaySetup,
					"tel",
					null,
					false,
					classes,
					placeholder,
					autoFillTokens,
					autoCompleteResource,
					null,
					action,
					triggersActionWhenItemSelected,
					valueChangedAction,
					pageModificationValue,
					validationPredicate,
					validationErrorNotifier ) );
		}

		/// <summary>
		/// Creates a setup object for a read-only telephone-number control.
		/// </summary>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the control.</param>
		/// <param name="validationPredicate"></param>
		/// <param name="validationErrorNotifier"></param>
		public static TelephoneNumberControlSetup CreateReadOnly(
			DisplaySetup displaySetup = null, ElementClassSet classes = null, Func<bool, bool> validationPredicate = null, Action validationErrorNotifier = null ) {
			return new TelephoneNumberControlSetup(
				new TextControlSetup(
					displaySetup,
					"tel",
					null,
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
					validationErrorNotifier ) );
		}

		internal TextControlSetup TextControlSetup { get; }

		private TelephoneNumberControlSetup( TextControlSetup textControlSetup ) {
			TextControlSetup = textControlSetup;
		}
	}
}