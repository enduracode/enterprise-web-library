using System;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.JavaScriptWriting;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The behavior for one or more clickable table elements. The click script will add rollover behavior to the table element(s), unless it is used on a field
	/// of an EWF table or an item of a column primary table. Column hover behavior is not possible with CSS.
	/// </summary>
	public class ElementActivationBehavior {
		/// <summary>
		/// Creates a script that redirects to the specified resource. Passing null for resourceInfo will result in no script being added.
		/// </summary>
		public static ElementActivationBehavior CreateRedirectScript( ResourceInfo resource ) {
			return new ElementActivationBehavior { resource = resource };
		}

		/// <summary>
		/// Creates a script that performs a post-back.
		/// </summary>
		/// <param name="postBack">Pass null to use the post-back corresponding to the first of the current data modifications.</param>
		public static ElementActivationBehavior CreatePostBackScript( PostBack postBack = null ) {
			return new ElementActivationBehavior { action = new PostBackFormAction( postBack ?? FormState.Current.PostBack ) };
		}

		/// <summary>
		/// Creates a custom script. A semicolon will be added to the end of the script. Do not pass null for script.
		/// </summary>
		public static ElementActivationBehavior CreateCustomScript( string script ) {
			return new ElementActivationBehavior { script = script };
		}

		private ResourceInfo resource;
		private FormAction action;
		private string script = "";

		private ElementActivationBehavior() {}

		internal void SetUpClickableControl( WebControl clickableControl ) {
			if( resource == null && action == null && script == "" )
				return;

			clickableControl.CssClass = clickableControl.CssClass.ConcatenateWithSpace( "ewfClickable" );

			if( resource != null && EwfPage.Instance.IsAutoDataUpdater ) {
				action = HyperlinkBehavior.GetHyperlinkPostBackAction( resource );
				resource = null;
			}

			Func<string> scriptGetter;
			if( resource != null )
				scriptGetter = () => "location.href = '" + EwfPage.Instance.GetClientUrl( resource.GetUrl() ) + "'; return false";
			else if( action != null ) {
				action.AddToPageIfNecessary();
				scriptGetter = () => action.GetJsStatements() + " return false";
			}
			else
				scriptGetter = () => script;

			// Defer script generation until after all controls have IDs.
			EwfPage.Instance.PreRender += delegate { clickableControl.AddJavaScriptEventScript( JsWritingMethods.onclick, scriptGetter() ); };
		}
	}
}