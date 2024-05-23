#nullable disable
namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// The configuration for a flow radio button.
/// </summary>
public class FlowRadioButtonSetup {
	/// <summary>
	/// Creates a setup object for a standard radio button.
	/// </summary>
	/// <param name="displaySetup"></param>
	/// <param name="classes">The classes on the container.</param>
	/// <param name="highlightedWhenSelected"></param>
	/// <param name="action">The action that will occur when the user hits Enter on the radio button. Pass null to use the current default action.</param>
	/// <param name="pageModificationValue"></param>
	/// <param name="nestedContentGetter">A function that gets the content that will appear beneath the radio button.</param>
	/// <param name="nestedContentAlwaysDisplayed">Pass true to force the nested content to always be displayed instead of only when the button is selected.
	/// </param>
	public static FlowRadioButtonSetup Create(
		DisplaySetup displaySetup = null, ElementClassSet classes = null, bool highlightedWhenSelected = false, SpecifiedValue<FormAction> action = null,
		PageModificationValue<bool> pageModificationValue = null, Func<IReadOnlyCollection<FlowComponent>> nestedContentGetter = null,
		bool nestedContentAlwaysDisplayed = false ) =>
		new(
			displaySetup,
			classes,
			RadioButtonSetup.Create( action: action, pageModificationValue: pageModificationValue ),
			highlightedWhenSelected,
			nestedContentGetter,
			nestedContentAlwaysDisplayed );

	/// <summary>
	/// Creates a setup object for a read-only radio button.
	/// </summary>
	/// <param name="displaySetup"></param>
	/// <param name="classes">The classes on the container.</param>
	/// <param name="highlightedWhenSelected"></param>
	/// <param name="nestedContentGetter">A function that gets the content that will appear beneath the radio button.</param>
	/// <param name="nestedContentAlwaysDisplayed">Pass true to force the nested content to always be displayed instead of only when the button is selected.
	/// </param>
	public static FlowRadioButtonSetup CreateReadOnly(
		DisplaySetup displaySetup = null, ElementClassSet classes = null, bool highlightedWhenSelected = false,
		Func<IReadOnlyCollection<FlowComponent>> nestedContentGetter = null, bool nestedContentAlwaysDisplayed = false ) =>
		new( displaySetup, classes, RadioButtonSetup.CreateReadOnly(), highlightedWhenSelected, nestedContentGetter, nestedContentAlwaysDisplayed );

	internal readonly DisplaySetup DisplaySetup;
	internal readonly ElementClassSet Classes;
	internal readonly RadioButtonSetup RadioButtonSetup;
	internal readonly bool HighlightedWhenSelected;
	internal readonly Func<IReadOnlyCollection<FlowComponent>> NestedContentGetter;
	internal readonly bool NestedContentAlwaysDisplayed;

	private FlowRadioButtonSetup(
		DisplaySetup displaySetup, ElementClassSet classes, RadioButtonSetup radioButtonSetup, bool highlightedWhenSelected,
		Func<IReadOnlyCollection<FlowComponent>> nestedContentGetter, bool nestedContentAlwaysDisplayed ) {
		DisplaySetup = displaySetup;
		Classes = classes;
		RadioButtonSetup = radioButtonSetup;
		HighlightedWhenSelected = highlightedWhenSelected;
		NestedContentGetter = nestedContentGetter;
		NestedContentAlwaysDisplayed = nestedContentAlwaysDisplayed;
	}

	internal FlowRadioButtonSetup AddPmv() =>
		new( DisplaySetup, Classes, RadioButtonSetup.AddPmv(), HighlightedWhenSelected, NestedContentGetter, NestedContentAlwaysDisplayed );
}