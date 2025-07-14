using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.ComponentDisplay;
using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.ElementBase.Classification;
using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.GeneralContentModels.Phrasing;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// The configuration for a button.
/// </summary>
public class ButtonSetup: ActionComponentSetup {
	public static implicit operator ActionComponentSetupsParameter( ButtonSetup? setup ) => new( setup is null ? [ ] : [ setup ] );

	private readonly Func<bool, Func<string, ActionComponentIcon?, ButtonStyle>, PhrasingComponent> buttonGetter;

	/// <summary>
	/// Creates a button setup object.
	/// </summary>
	/// <param name="text">Do not pass null or the empty string.</param>
	/// <param name="displaySetup"></param>
	/// <param name="behavior">The behavior. Pass null to use the form default action.</param>
	/// <param name="classes">The classes on the button.</param>
	/// <param name="icon">The icon.</param>
	public ButtonSetup(
		string text, DisplaySetup? displaySetup = null, ButtonBehavior? behavior = null, ElementClassSet? classes = null, ActionComponentIcon? icon = null ) {
		behavior ??= new FormActionBehavior( FormState.Current.DefaultAction );

		DisplaySetup = displaySetup;
		buttonGetter = ( enableSubmitButton, buttonStyleSelector ) => {
			var postBack = !enableSubmitButton ? null :
			               behavior is FormActionBehavior formActionBehavior ? ( formActionBehavior.Action as PostBackFormAction )?.PostBack :
			               behavior is PostBackBehavior postBackBehavior ? postBackBehavior.PostBackAction.PostBack : null;
			return postBack != null
				       ? new SubmitButton( buttonStyleSelector( text, icon ), classes: classes, postBack: postBack )
				       : new EwfButton( buttonStyleSelector( text, icon ), behavior: behavior, classes: classes );
		};
	}

	/// <inheritdoc/>
	public DisplaySetup? DisplaySetup { get; }

	/// <inheritdoc/>
	public PhrasingComponent GetActionComponent(
		Func<string, ActionComponentIcon?, HyperlinkStyle>? hyperlinkStyleSelector, Func<string, ActionComponentIcon?, ButtonStyle> buttonStyleSelector,
		bool enableSubmitButton = false ) =>
		buttonGetter( enableSubmitButton, buttonStyleSelector );
}

public class ButtonSetupsParameter {
	public static implicit operator ButtonSetupsParameter( ButtonSetup? setup ) => new( setup is null ? [ ] : [ setup ] );

	public static implicit operator ActionComponentSetupsParameter?( ButtonSetupsParameter? setup ) =>
		setup is null ? null : new ActionComponentSetupsParameter( setup.sequence );

	private readonly IEnumerable<ButtonSetup> sequence;
	internal readonly Lazy<IReadOnlyCollection<ButtonSetup>> Collection;

	internal ButtonSetupsParameter( IEnumerable<ButtonSetup> sequence ) {
		this.sequence = sequence;
		Collection = new Lazy<IReadOnlyCollection<ButtonSetup>>( sequence.Materialize );
	}

	/// <summary>
	/// Returns a new parameter with this parameter’s button setups plus the specified setups.
	/// </summary>
	public ButtonSetupsParameter Add( ButtonSetupsParameter buttonSetups ) => new( sequence.Concat( buttonSetups.sequence ) );
}

[ PublicAPI ]
public static class ButtonSetupsParameterExtensionCreators {
	/// <summary>
	/// Returns a parameter with this button setup plus the specified setups.
	/// </summary>
	public static ButtonSetupsParameter Add( this ButtonSetup buttonSetup, ButtonSetupsParameter buttonSetups ) =>
		new ButtonSetupsParameter( [ buttonSetup ] ).Add( buttonSetups );

	/// <summary>
	/// Returns a parameter with the button setups in this sequence.
	/// </summary>
	public static ButtonSetupsParameter ToParameter( this IEnumerable<ButtonSetup> buttonSetups ) => new( buttonSetups );
}