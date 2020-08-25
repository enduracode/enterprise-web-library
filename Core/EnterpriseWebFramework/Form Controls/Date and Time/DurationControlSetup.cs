using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The configuration for a duration control.
	/// </summary>
	public class DurationControlSetup {
		/// <summary>
		/// Creates a setup object for a standard duration control.
		/// </summary>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the control container.</param>
		/// <param name="action">The action that will occur when the user hits Enter on the control. Pass null to use the current default action.</param>
		/// <param name="valueChangedAction">The action that will occur when the value is changed. Pass null for no action.</param>
		/// <param name="pageModificationValue"></param>
		/// <param name="validationPredicate"></param>
		/// <param name="validationErrorNotifier"></param>
		public static DurationControlSetup Create(
			DisplaySetup displaySetup = null, ElementClassSet classes = null, SpecifiedValue<FormAction> action = null, FormAction valueChangedAction = null,
			PageModificationValue<string> pageModificationValue = null, Func<bool, bool> validationPredicate = null, Action validationErrorNotifier = null ) {
			return new DurationControlSetup(
				displaySetup,
				false,
				classes,
				action,
				valueChangedAction,
				pageModificationValue,
				validationPredicate,
				validationErrorNotifier );
		}

		/// <summary>
		/// Creates a setup object for a read-only duration control.
		/// </summary>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the control container.</param>
		/// <param name="validationPredicate"></param>
		/// <param name="validationErrorNotifier"></param>
		public static DurationControlSetup CreateReadOnly(
			DisplaySetup displaySetup = null, ElementClassSet classes = null, Func<bool, bool> validationPredicate = null, Action validationErrorNotifier = null ) {
			return new DurationControlSetup( displaySetup, true, classes, null, null, null, validationPredicate, validationErrorNotifier );
		}

		internal readonly DisplaySetup DisplaySetup;
		internal readonly bool IsReadOnly;
		internal readonly ElementClassSet Classes;
		internal readonly FormAction Action;
		internal readonly FormAction ValueChangedAction;
		internal readonly PageModificationValue<string> PageModificationValue;
		internal readonly Func<bool, bool> ValidationPredicate;
		internal readonly Action ValidationErrorNotifier;

		internal DurationControlSetup(
			DisplaySetup displaySetup, bool isReadOnly, ElementClassSet classes, SpecifiedValue<FormAction> action, FormAction valueChangedAction,
			PageModificationValue<string> pageModificationValue, Func<bool, bool> validationPredicate, Action validationErrorNotifier ) {
			DisplaySetup = displaySetup;
			IsReadOnly = isReadOnly;
			Classes = classes;
			Action = action != null ? action.Value : FormState.Current.FormControlDefaultAction;
			ValueChangedAction = valueChangedAction;
			PageModificationValue = pageModificationValue;
			ValidationPredicate = validationPredicate;
			ValidationErrorNotifier = validationErrorNotifier;
		}
	}
}