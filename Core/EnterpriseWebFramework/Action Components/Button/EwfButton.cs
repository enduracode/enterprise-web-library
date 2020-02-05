using System;
using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A button.
	/// </summary>
	public sealed class EwfButton: PhrasingComponent {
		private readonly IReadOnlyCollection<FlowComponent> children;

		/// <summary>
		/// Creates a button.
		/// </summary>
		/// <param name="style">The style.</param>
		/// <param name="displaySetup"></param>
		/// <param name="behavior">The behavior. Pass null to use the form default action. If you need to add additional JavaScript action statements to any
		/// behavior besides <see cref="CustomButtonBehavior"/>, we recommend using the jQuery document ready event to add an additional click handler to the
		/// appropriate button element(s), which you can select using a class if there is no simpler way.</param>
		/// <param name="classes">The classes on the button.</param>
		public EwfButton( ButtonStyle style, DisplaySetup displaySetup = null, ButtonBehavior behavior = null, ElementClassSet classes = null ) {
			behavior = behavior ?? new FormActionBehavior( FormState.Current.DefaultAction );
			var elementChildren = style.GetChildren();
			var elementEtherealChildren = behavior.GetEtherealChildren();
			children = new DisplayableElement(
				context => {
					behavior.AddPostBack();
					return new DisplayableElementData(
						displaySetup,
						() => new DisplayableElementLocalData(
							"button",
							new FocusabilityCondition( true ),
							isFocused => {
								var attributes = new List<Tuple<string, string>> { Tuple.Create( "type", "button" ) };
								attributes.AddRange( behavior.GetAttributes() );
								if( isFocused )
									attributes.Add( Tuple.Create( "autofocus", "autofocus" ) );

								return new DisplayableElementFocusDependentData(
									attributes: attributes,
									includeIdAttribute: behavior.IncludesIdAttribute(),
									jsInitStatements: behavior.GetJsInitStatements( context.Id ) + style.GetJsInitStatements( context.Id ) );
							} ),
						classes: style.GetClasses().Add( classes ?? ElementClassSet.Empty ),
						children: elementChildren,
						etherealChildren: elementEtherealChildren );
				} ).ToCollection();
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}