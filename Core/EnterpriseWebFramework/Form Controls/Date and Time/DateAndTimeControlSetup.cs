﻿#nullable disable
using NodaTime;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// The configuration for a date-and-time control.
/// </summary>
public class DateAndTimeControlSetup {
	/// <summary>
	/// Creates a setup object for a standard date-and-time control.
	/// </summary>
	/// <param name="displaySetup"></param>
	/// <param name="classes">The classes on the control.</param>
	/// <param name="autoFillTokens">A list of auto-fill detail tokens (see
	/// https://html.spec.whatwg.org/multipage/form-control-infrastructure.html#autofill-detail-tokens), or "off" to instruct the browser to disable auto-fill
	/// (see https://stackoverflow.com/a/23234498/35349 for an explanation of why this could be ignored). Do not pass null.</param>
	/// <param name="minuteInterval">Affects the slider but does not prevent other values from passing validation.</param>
	/// <param name="action">The action that will occur when the user hits Enter on the control. Pass null to use the current default action.</param>
	/// <param name="valueChangedAction">The action that will occur when the value is changed. Pass null for no action.</param>
	/// <param name="datePageModificationValue"></param>
	/// <param name="timePageModificationValue"></param>
	/// <param name="validationPredicate"></param>
	/// <param name="validationErrorNotifier"></param>
	public static DateAndTimeControlSetup Create(
		DisplaySetup displaySetup = null, ElementClassSet classes = null, string autoFillTokens = "", int minuteInterval = 15,
		SpecifiedValue<FormAction> action = null, FormAction valueChangedAction = null, PageModificationValue<LocalDate?> datePageModificationValue = null,
		PageModificationValue<string> timePageModificationValue = null, Func<bool, bool> validationPredicate = null, Action validationErrorNotifier = null ) =>
		new(
			displaySetup,
			false,
			classes,
			autoFillTokens,
			minuteInterval,
			action,
			valueChangedAction,
			datePageModificationValue,
			timePageModificationValue,
			validationPredicate,
			validationErrorNotifier );

	/// <summary>
	/// Creates a setup object for a read-only date-and-time control.
	/// </summary>
	/// <param name="displaySetup"></param>
	/// <param name="classes">The classes on the control.</param>
	/// <param name="validationPredicate"></param>
	/// <param name="validationErrorNotifier"></param>
	public static DateAndTimeControlSetup CreateReadOnly(
		DisplaySetup displaySetup = null, ElementClassSet classes = null, Func<bool, bool> validationPredicate = null, Action validationErrorNotifier = null ) =>
		new( displaySetup, true, classes, "", null, null, null, null, null, validationPredicate, validationErrorNotifier );

	internal readonly DisplaySetup DisplaySetup;
	internal readonly bool IsReadOnly;
	internal readonly ElementClassSet Classes;
	internal readonly string AutoFillTokens;
	internal readonly int? MinuteInterval;
	internal readonly FormAction Action;
	internal readonly FormAction ValueChangedAction;
	internal readonly PageModificationValue<LocalDate?> DatePageModificationValue;
	internal readonly PageModificationValue<string> TimePageModificationValue;
	internal readonly Func<bool, bool> ValidationPredicate;
	internal readonly Action ValidationErrorNotifier;

	internal DateAndTimeControlSetup(
		DisplaySetup displaySetup, bool isReadOnly, ElementClassSet classes, string autoFillTokens, int? minuteInterval, SpecifiedValue<FormAction> action,
		FormAction valueChangedAction, PageModificationValue<LocalDate?> datePageModificationValue, PageModificationValue<string> timePageModificationValue,
		Func<bool, bool> validationPredicate, Action validationErrorNotifier ) {
		DisplaySetup = displaySetup;
		IsReadOnly = isReadOnly;
		Classes = classes;
		if( autoFillTokens.Length > 0 )
			throw new NotSupportedException( "Auto-fill detail tokens are not supported with the current implementation of the date-and-time control." );
		AutoFillTokens = autoFillTokens;
		MinuteInterval = minuteInterval;
		Action = action != null ? action.Value : FormState.Current.FormControlDefaultAction;
		ValueChangedAction = valueChangedAction;
		DatePageModificationValue = datePageModificationValue;
		TimePageModificationValue = timePageModificationValue;
		ValidationPredicate = validationPredicate;
		ValidationErrorNotifier = validationErrorNotifier;
	}
}