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
		/// Creates a script that posts the page back and executes the specified method. Do not pass null for method.
		/// </summary>
		public static ClickScript CreatePostBackScript( Action method ) {
			return new ClickScript { method = method };
		}

		/// <summary>
		/// Creates a custom script. A semicolon will be added to the end of the script. Do not pass null for script.
		/// </summary>
		public static ClickScript CreateCustomScript( string script ) {
			return new ClickScript { script = script };
		}

		private PageInfo page;
		private Action method;
		private string script = "";

		private ClickScript() {}

		internal void SetUpClickableControl( WebControl clickableControl ) {
			if( page == null && method == null && script == "" )
				return;

			clickableControl.CssClass = clickableControl.CssClass.ConcatenateWithSpace( "ewfClickable" );

			if( page != null && EwfPage.Instance.IsAutoDataModifier ) {
				var pageCopy = page;
				page = null;
				method = () => EwfPage.Instance.EhRedirect( pageCopy );
			}

			Func<string> scriptGetter;
			if( page != null )
				scriptGetter = () => "location.href = '" + EwfPage.Instance.GetClientUrl( page.GetUrl() ) + "'; return false";
			else if( method != null ) {
				var externalHandler = new ExternalPostBackEventHandler( method );

				// NOTE: Remove this hack when DynamicTable is gone.
				if( clickableControl is TableRow )
					clickableControl.Parent.Parent.Controls.Add( externalHandler );
				else
					clickableControl.Controls.Add( externalHandler );

				scriptGetter = () => PostBackButton.GetPostBackScript( externalHandler, true );
			}
			else
				scriptGetter = () => script;

			// Defer script generation until after all controls have IDs.
			EwfPage.Instance.PreRender += delegate { clickableControl.AddJavaScriptEventScript( JsWritingMethods.onclick, scriptGetter() ); };
		}
	}
}