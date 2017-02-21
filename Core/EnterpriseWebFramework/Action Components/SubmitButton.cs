using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.JavaScriptWriting;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A submit button.
	/// </summary>
	public class SubmitButton: PhrasingComponent {
		/// <summary>
		/// Ensures that the specified control will submit the form when the enter key is pressed while the control has focus. If you specify the submit-button
		/// post-back, this method relies on HTML's built-in implicit submission behavior, which will simulate a click on the submit button.
		/// </summary>
		/// <param name="control"></param>
		/// <param name="postBack">Do not pass null.</param>
		/// <param name="forceJsHandling"></param>
		/// <param name="predicate"></param>
		internal static void EnsureImplicitSubmission( WebControl control, PostBack postBack, bool forceJsHandling, string predicate = "" ) {
			// EWF does not allow form controls to use HTML's built-in implicit submission on a page with no submit button. There are two reasons for this. First, the
			// behavior of HTML's implicit submission appears to be somewhat arbitrary when there is no submit button; see
			// http://www.whatwg.org/specs/web-apps/current-work/multipage/association-of-controls-and-forms.html#implicit-submission. Second, we don't want the
			// implicit submission behavior of form controls to unpredictably change if a submit button is added or removed.
			if( postBack != EwfPage.Instance.SubmitButtonPostBack || forceJsHandling )
				control.AddJavaScriptEventScript(
					JsWritingMethods.onkeypress,
					"if( event.which == 13 " + predicate.PrependDelimiter( " && " ) + " ) { " + EwfPage.GetPostBackScript( postBack ) + "; }" );
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
			postBack = postBack ?? EwfPage.PostBack;

			children = new DisplayableElement(
				context => {
					EwfPage.Instance.AddPostBack( postBack );

					if( EwfPage.Instance.SubmitButtonPostBack != null )
						throw new ApplicationException( "A submit button already exists on the page." );
					EwfPage.Instance.SubmitButtonPostBack = postBack;

					return new DisplayableElementData(
						displaySetup,
						() =>
						new DisplayableElementLocalData(
							"button",
							attributes:
							Tuple.Create( "type", "button" ).ToCollection().Concat( new[] { Tuple.Create( "name", EwfPage.ButtonElementName ), Tuple.Create( "value", "v" ) } ),
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