using System;
using System.Collections.Generic;
using Humanizer;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A submit button.
	/// </summary>
	public class SubmitButton: PhrasingComponent {
		/// <summary>
		/// Returns the JavaScript statements that should be executed on keypress to ensure that a form control will trigger the specified action when the enter key
		/// is pressed while the control has focus. If you specify the submit-button post-back, this method relies on HTML’s built-in implicit submission behavior,
		/// which will simulate a click on the submit button.
		/// </summary>
		internal static string GetImplicitSubmissionKeyPressStatements( FormAction action, bool forceJsHandling ) {
			// EWF does not allow form controls to use HTML’s built-in implicit submission on a page with no submit button. There are two reasons for this. First, the
			// behavior of HTML’s implicit submission appears to be somewhat arbitrary when there is no submit button; see
			// https://html.spec.whatwg.org/multipage/form-control-infrastructure.html#implicit-submission. Second, we don’t want the implicit submission behavior of
			// form controls to unpredictably change if a submit button is added or removed.
			return action is PostBackFormAction postBackAction && postBackAction.PostBack == EwfPage.Instance.SubmitButtonPostBack && !forceJsHandling
				       ? ""
				       : "if( e.which == 13 ) {{ {0} }}".FormatWith( "e.preventDefault();".ConcatenateWithSpace( action?.GetJsStatements() ?? "" ) );
		}

		private readonly IReadOnlyCollection<FlowComponent> children;

		/// <summary>
		/// Creates a submit button.
		/// </summary>
		/// <param name="style">The style.</param>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the button.</param>
		/// <param name="postBack">Pass null to use the post-back corresponding to the first of the current data modifications.</param>
		public SubmitButton( ButtonStyle style, DisplaySetup displaySetup = null, ElementClassSet classes = null, PostBack postBack = null ) {
			var elementChildren = style.GetChildren();
			var postBackAction = new PostBackFormAction( postBack ?? FormState.Current.PostBack );

			children = new DisplayableElement(
				context => {
					FormAction action = postBackAction;
					action.AddToPageIfNecessary();

					if( EwfPage.Instance.SubmitButtonPostBack != null )
						throw new ApplicationException( "A submit button already exists on the page." );
					EwfPage.Instance.SubmitButtonPostBack = postBackAction.PostBack;

					return new DisplayableElementData(
						displaySetup,
						() => new DisplayableElementLocalData(
							"button",
							new FocusabilityCondition( true ),
							isFocused => {
								var attributes = new List<ElementAttribute> { new ElementAttribute( "name", EwfPage.ButtonElementName ), new ElementAttribute( "value", "v" ) };
								if( isFocused )
									attributes.Add( new ElementAttribute( "autofocus" ) );

								return new DisplayableElementFocusDependentData( attributes: attributes, jsInitStatements: style.GetJsInitStatements( context.Id ) );
							} ),
						classes: style.GetClasses().Add( classes ?? ElementClassSet.Empty ),
						children: elementChildren );
				} ).ToCollection();
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}