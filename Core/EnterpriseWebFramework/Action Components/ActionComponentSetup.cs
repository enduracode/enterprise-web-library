using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.ComponentDisplay;
using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.GeneralContentModels.Phrasing;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// The configuration for an action component.
/// </summary>
public interface ActionComponentSetup {
	/// <summary>
	/// EWF use only.
	/// </summary>
	DisplaySetup? DisplaySetup { get; }

	/// <summary>
	/// EWF use only.
	/// </summary>
	PhrasingComponent? GetActionComponent(
		Func<string, ActionComponentIcon?, HyperlinkStyle> hyperlinkStyleSelector, Func<string, ActionComponentIcon?, ButtonStyle> buttonStyleSelector,
		bool enableSubmitButton = false );
}

public class ActionComponentSetupsParameter {
	private readonly IEnumerable<ActionComponentSetup> sequence;
	internal readonly Lazy<IReadOnlyCollection<ActionComponentSetup>> Collection;

	internal ActionComponentSetupsParameter( IEnumerable<ActionComponentSetup> sequence ) {
		this.sequence = sequence;
		Collection = new Lazy<IReadOnlyCollection<ActionComponentSetup>>( sequence.Materialize );
	}

	/// <summary>
	/// Returns a new parameter with this parameter’s action-component setups plus the specified setups.
	/// </summary>
	public ActionComponentSetupsParameter Add( ActionComponentSetupsParameter actionComponentSetups ) => new( sequence.Concat( actionComponentSetups.sequence ) );
}

[ PublicAPI ]
public static class ActionComponentSetupsParameterExtensionCreators {
	/// <summary>
	/// Returns a parameter with this action-component setup plus the specified setups.
	/// </summary>
	public static ActionComponentSetupsParameter Add( this ActionComponentSetup? actionComponentSetup, ActionComponentSetupsParameter actionComponentSetups ) =>
		new ActionComponentSetupsParameter( actionComponentSetup is null ? [ ] : [ actionComponentSetup ] ).Add( actionComponentSetups );

	/// <summary>
	/// Returns a parameter with the action-component setups in this sequence.
	/// </summary>
	public static ActionComponentSetupsParameter ToParameter( this IEnumerable<ActionComponentSetup> actionComponentSetups ) => new( actionComponentSetups );
}