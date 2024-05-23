#nullable disable
namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// The configuration for an imprecise-number control.
/// </summary>
public class ImpreciseNumberControlSetup {
	/// <summary>
	/// Creates a setup object for a standard imprecise-number control.
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
	public static ImpreciseNumberControlSetup Create(
		DisplaySetup displaySetup = null, ElementClassSet classes = null, string autoFillTokens = "", SpecifiedValue<FormAction> action = null,
		FormAction valueChangedAction = null, PageModificationValue<decimal> pageModificationValue = null, Func<bool, bool> validationPredicate = null,
		Action validationErrorNotifier = null ) {
		return new ImpreciseNumberControlSetup(
			new NumberControlSetup(
				displaySetup,
				true,
				false,
				classes,
				"",
				autoFillTokens,
				null,
				action,
				null,
				valueChangedAction,
				pageModificationValue,
				validationPredicate,
				validationErrorNotifier ) );
	}

	/// <summary>
	/// Creates a setup object for a read-only imprecise-number control.
	/// </summary>
	/// <param name="displaySetup"></param>
	/// <param name="classes">The classes on the control.</param>
	/// <param name="validationPredicate"></param>
	/// <param name="validationErrorNotifier"></param>
	public static ImpreciseNumberControlSetup CreateReadOnly(
		DisplaySetup displaySetup = null, ElementClassSet classes = null, Func<bool, bool> validationPredicate = null, Action validationErrorNotifier = null ) {
		return new ImpreciseNumberControlSetup(
			new NumberControlSetup( displaySetup, true, true, classes, "", "", null, null, null, null, null, validationPredicate, validationErrorNotifier ) );
	}

	internal NumberControlSetup NumberControlSetup { get; }

	private ImpreciseNumberControlSetup( NumberControlSetup numberControlSetup ) {
		NumberControlSetup = numberControlSetup;
	}
}