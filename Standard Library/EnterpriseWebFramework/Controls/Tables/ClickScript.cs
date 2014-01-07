using System;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.JavaScriptWriting;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// The behavior for one or more clickable table elements. The click script will add rollover behavior to the table element(s), unless it is used on a field
	/// of an EWF table or an item of a column primary table. Column hover behavior is not possible with CSS.
	/// </summary>
	public class ClickScript {
		/// <summary>
		/// Creates a script that redirects to the specified page. Passing null for pageInfo will result in no script being added.
		/// </summary>
		public static ClickScript CreateRedirectScript( PageInfo page ) {
			return new ClickScript { page = page };
		}

		/// <summary>
		/// Creates a script that performs a post-back.
		/// </summary>
		/// <param name="postBack">Do not pass null.</param>
		public static ClickScript CreatePostBackScript( PostBack postBack ) {
			return new ClickScript { postBack = postBack };
		}

		/// <summary>
		/// Creates a custom script. A semicolon will be added to the end of the script. Do not pass null for script.
		/// </summary>
		public static ClickScript CreateCustomScript( string script ) {
			return new ClickScript { script = script };
		}

		private PageInfo page;
		private PostBack postBack;
		private string script = "";

		private ClickScript() {}

		internal void SetUpClickableControl( WebControl clickableControl ) {
			if( page == null && postBack == null && script == "" )
				return;

			clickableControl.CssClass = clickableControl.CssClass.ConcatenateWithSpace( "ewfClickable" );

			if( page != null && EwfPage.Instance.IsAutoDataUpdater ) {
				postBack = EwfLink.GetLinkPostBack( page );
				page = null;
			}

			Func<string> scriptGetter;
			if( page != null )
				scriptGetter = () => "location.href = '" + EwfPage.Instance.GetClientUrl( page.GetUrl() ) + "'; return false";
			else if( postBack != null ) {
				EwfPage.Instance.AddPostBack( postBack );
				scriptGetter = () => PostBackButton.GetPostBackScript( postBack );
			}
			else
				scriptGetter = () => script;

			// Defer script generation until after all controls have IDs.
			EwfPage.Instance.PreRender += delegate { clickableControl.AddJavaScriptEventScript( JsWritingMethods.onclick, scriptGetter() ); };
		}
	}
}