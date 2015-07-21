using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.JavaScriptWriting;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A control that, when clicked, executes custom JavaScript.
	/// </summary>
	public class CustomButton: WebControl, ControlTreeDataLoader, ControlWithJsInitLogic, ActionControl {
		/// <summary>
		/// Sets the display style of this button. Do not set this to null.
		/// Choices are: ButtonActionControlStyle (default), BoxActionControlStyle, ImageActionControlStyle,CustomActionControlStyle, and TextActionControlStyle.
		/// </summary>
		public ActionControlStyle ActionControlStyle { private get; set; }

		/// <summary>
		/// Creates a custom button. A semicolon will be added to the end of the script.
		/// </summary>
		public CustomButton( Func<string> scriptGetter ) {
			ActionControlStyle = new ButtonActionControlStyle( "" );

			// Defer script generation until after all controls have IDs.
			PreRender += delegate { this.AddJavaScriptEventScript( JsWritingMethods.onclick, scriptGetter() ); };
		}

		void ControlTreeDataLoader.LoadData() {
			if( TagKey == HtmlTextWriterTag.Button )
				PostBackButton.AddButtonAttributes( this );
			CssClass = CssClass.ConcatenateWithSpace( "ewfClickable" );
			ActionControlStyle.SetUpControl( this, "", Unit.Empty, Unit.Empty, width => { } );
		}

		string ControlWithJsInitLogic.GetJsInitStatements() {
			return ActionControlStyle.GetJsInitStatements( this );
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return PostBackButton.GetTagKey( ActionControlStyle ); } }
	}
}