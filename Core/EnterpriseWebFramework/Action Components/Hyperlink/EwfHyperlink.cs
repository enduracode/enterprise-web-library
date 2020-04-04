using System.Collections.Generic;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A hyperlink.
	/// </summary>
	public sealed class EwfHyperlink: PhrasingComponent {
		private readonly IReadOnlyCollection<FlowComponent> children;

		/// <summary>
		/// Creates a hyperlink.
		/// </summary>
		/// <param name="behavior">The behavior. Pass a <see cref="ResourceInfo"/> to navigate to the resource in the default way, or call
		/// <see cref="HyperlinkBehaviorExtensionCreators.ToHyperlinkNewTabBehavior(ResourceInfo)"/> or
		/// <see cref="HyperlinkBehaviorExtensionCreators.ToHyperlinkModalBoxBehavior(ResourceInfo, BrowsingContextSetup)"/>. For a mailto link, call
		/// <see cref="HyperlinkBehaviorExtensionCreators.ToHyperlinkBehavior(Email.EmailAddress, string, string, string, string)"/>. If you need to add additional
		/// JavaScript action statements to any behavior, we recommend using the jQuery document ready event to add a click handler to the appropriate a element(s),
		/// which you can select using a class if there is no simpler way.</param>
		/// <param name="style">The style.</param>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the hyperlink.</param>
		public EwfHyperlink( HyperlinkBehavior behavior, HyperlinkStyle style, DisplaySetup displaySetup = null, ElementClassSet classes = null ) {
			children = new DisplayableElement(
				context => {
					behavior.PostBackAdder();
					return new DisplayableElementData(
						displaySetup,
						() => {
							DisplayableElementFocusDependentData getFocusDependentData( bool isFocused ) =>
								new DisplayableElementFocusDependentData(
									attributes: behavior.AttributeGetter( false ),
									includeIdAttribute: behavior.IncludesIdAttribute( false ) || isFocused,
									jsInitStatements: StringTools.ConcatenateWithDelimiter(
										" ",
										behavior.JsInitStatementGetter( context.Id, false ),
										style.GetJsInitStatements( context.Id ),
										isFocused ? "document.getElementById( '{0}' ).focus();".FormatWith( context.Id ) : "" ) );

							return behavior.IsFocusable
								       ? new DisplayableElementLocalData( "a", new FocusabilityCondition( true ), getFocusDependentData )
								       : new DisplayableElementLocalData( "a", focusDependentData: getFocusDependentData( false ) );
						},
						classes: behavior.Classes.Add( style.GetClasses() ).Add( classes ?? ElementClassSet.Empty ),
						children: style.GetChildren( behavior.Url.Value ),
						etherealChildren: behavior.EtherealChildren );
				} ).ToCollection();
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}