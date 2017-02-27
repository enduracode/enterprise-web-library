using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.JavaScriptWriting;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A submit button.
	/// </summary>
	public class SubmitButton: PhrasingComponent {
		/// <summary>
		/// Ensures that the specified control will trigger the specified action when the enter key is pressed while the control has focus. If you specify the
		/// submit-button post-back, this method relies on HTML's built-in implicit submission behavior, which will simulate a click on the submit button.
		/// </summary>
		/// <param name="control"></param>
		/// <param name="action">Do not pass null.</param>
		/// <param name="forceJsHandling"></param>
		/// <param name="predicate"></param>
		internal static void EnsureImplicitSubmissionAction( WebControl control, FormAction action, bool forceJsHandling, string predicate = "" ) {
			var postBackAction = action as PostBackFormAction;

			// EWF does not allow form controls to use HTML's built-in implicit submission on a page with no submit button. There are two reasons for this. First, the
			// behavior of HTML's implicit submission appears to be somewhat arbitrary when there is no submit button; see
			// https://html.spec.whatwg.org/multipage/forms.html#implicit-submission. Second, we don't want the implicit submission behavior of form controls to
			// unpredictably change if a submit button is added or removed.
			if( postBackAction == null || postBackAction.PostBack != EwfPage.Instance.SubmitButtonPostBack || forceJsHandling )
				control.AddJavaScriptEventScript(
					JsWritingMethods.onkeypress,
					"if( event.which == 13 " + predicate.PrependDelimiter( " && " ) + " ) { " + action.GetJsStatements() + " return false; }" );
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
						() =>
						new DisplayableElementLocalData(
							"button",
							attributes: new[] { Tuple.Create( "name", EwfPage.ButtonElementName ), Tuple.Create( "value", "v" ) },
							jsInitStatements: style.GetJsInitStatements( context.Id ) ),
						classes: style.GetClasses().Add( classes ?? ElementClassSet.Empty ),
						children: elementChildren );
				} ).ToCollection();
		}

		IEnumerable<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}