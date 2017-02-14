using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A hyperlink.
	/// </summary>
	public sealed class EwfHyperlink: PhrasingComponent {
		private readonly IReadOnlyCollection<FlowComponentOrNode> children;

		/// <summary>
		/// Creates a hyperlink.
		/// </summary>
		/// <param name="behavior">The behavior. Pass a <see cref="ResourceInfo"/> to navigate to the resource in the default way, or call
		/// <see cref="HyperlinkBehaviorExtensionCreators.ToHyperlinkNewTabBehavior(ResourceInfo)"/>. For a mailto link, call
		/// <see cref="HyperlinkBehaviorExtensionCreators.ToHyperlinkBehavior(Email.EmailAddress, string, string, string, string)"/>.</param>
		/// <param name="style">The style.</param>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the hyperlink.</param>
		public EwfHyperlink( HyperlinkBehavior behavior, HyperlinkStyle style, DisplaySetup displaySetup = null, ElementClassSet classes = null ) {
			children = new DisplayableElement(
				context => {
					behavior.PostBackAdder();
					return new DisplayableElementData(
						displaySetup,
						() =>
						new DisplayableElementLocalData(
							"a",
							attributes: behavior.AttributeGetter(),
							includeIdAttribute: behavior.IncludeIdAttribute,
							jsInitStatements: behavior.JsInitStatementGetter( context.Id ) + style.GetJsInitStatements( context.Id ) ),
						classes: behavior.Classes.Add( style.GetClasses() ).Add( classes ?? ElementClassSet.Empty ),
						children: style.GetChildren( behavior.Url ),
						etherealChildren: behavior.EtherealChildren );
				} ).ToCollection();
		}

		IEnumerable<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}