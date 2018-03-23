using System;
using System.Collections.Generic;
using System.Linq;

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
		/// <param name="behavior">The behavior. Pass null to use the form default action.</param>
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
							focusDependentData: new DisplayableElementFocusDependentData(
								attributes: Tuple.Create( "type", "button" ).ToCollection().Concat( behavior.GetAttributes() ),
								includeIdAttribute: behavior.IncludesIdAttribute(),
								jsInitStatements: behavior.GetJsInitStatements( context.Id ) + style.GetJsInitStatements( context.Id ) ) ),
						classes: style.GetClasses().Add( classes ?? ElementClassSet.Empty ),
						children: elementChildren,
						etherealChildren: elementEtherealChildren );
				} ).ToCollection();
		}

		IEnumerable<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}