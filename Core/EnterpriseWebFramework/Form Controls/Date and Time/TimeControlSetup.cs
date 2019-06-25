using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The configuration for a time control.
	/// </summary>
	public class TimeControlSetup {
		/// <summary>
		/// Creates a setup object for a standard time control.
		/// </summary>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the control.</param>
		/// <param name="autoFillTokens">A list of auto-fill detail tokens (see
		/// https://html.spec.whatwg.org/multipage/form-control-infrastructure.html#autofill-detail-tokens), or "off" to instruct the browser to disable auto-fill
		/// (see https://stackoverflow.com/a/23234498/35349 for an explanation of why this could be ignored). Do not pass null.</param>
		/// <param name="action">The action that will occur when the user hits Enter on the control. Pass null to use the current default action.</param>
		/// <param name="valueChangedAction">The action that will occur when the value is changed. Pass null for no action.</param>
		/// <param name="pageModificationValue"></param>
		/// <param name="validationPredicate"></param>
		/// <param name="validationErrorNotifier"></param>
		public static TimeControlSetup Create(
			DisplaySetup displaySetup = null, ElementClassSet classes = null, string autoFillTokens = "", FormAction action = null,
			FormAction valueChangedAction = null, PageModificationValue<string> pageModificationValue = null, Func<bool, bool> validationPredicate = null,
			Action validationErrorNotifier = null ) {
			return new TimeControlSetup(
				displaySetup,
				false,
				classes,
				autoFillTokens,
				action,
				valueChangedAction,
				pageModificationValue,
				validationPredicate,
				validationErrorNotifier );
		}

		/// <summary>
		/// Creates a setup object for a read-only time control.
		/// </summary>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the control.</param>
		/// <param name="validationPredicate"></param>
		/// <param name="validationErrorNotifier"></param>
		public static TimeControlSetup CreateReadOnly(
			DisplaySetup displaySetup = null, ElementClassSet classes = null, Func<bool, bool> validationPredicate = null, Action validationErrorNotifier = null ) {
			return new TimeControlSetup( displaySetup, true, classes, "", null, null, null, validationPredicate, validationErrorNotifier );
		}

		internal readonly DisplaySetup DisplaySetup;
		internal readonly bool IsReadOnly;
		internal readonly ElementClassSet Classes;
		internal readonly string AutoFillTokens;
		internal readonly FormAction Action;
		internal readonly FormAction ValueChangedAction;
		internal readonly PageModificationValue<string> PageModificationValue;
		internal readonly Func<bool, bool> ValidationPredicate;
		internal readonly Action ValidationErrorNotifier;

		internal TimeControlSetup(
			DisplaySetup displaySetup, bool isReadOnly, ElementClassSet classes, string autoFillTokens, FormAction action, FormAction valueChangedAction,
			PageModificationValue<string> pageModificationValue, Func<bool, bool> validationPredicate, Action validationErrorNotifier ) {
			DisplaySetup = displaySetup;
			IsReadOnly = isReadOnly;
			Classes = classes;
			AutoFillTokens = autoFillTokens;
			Action = action ?? FormState.Current.DefaultAction;
			ValueChangedAction = valueChangedAction;
			PageModificationValue = pageModificationValue;
			ValidationPredicate = validationPredicate;
			ValidationErrorNotifier = validationErrorNotifier;
		}
	}
}