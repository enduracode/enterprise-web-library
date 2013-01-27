using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.JavaScriptWriting;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A control that, when clicked, executes custom JavaScript.
	/// </summary>
	public class CustomButton: WebControl, ControlTreeDataLoader, ControlWithJsInitLogic, ActionControl {
		private readonly string script;

		/// <summary>
		/// Sets the display style of this button. Do not set this to null.
		/// Choices are: ButtonActionControlStyle (default), BoxActionControlStyle, ImageActionControlStyle,CustomActionControlStyle, and TextActionControlStyle.
		/// </summary>
		public ActionControlStyle ActionControlStyle { private get; set; }

		/// <summary>
		/// Creates a custom button. A semicolon will be added to the end of the script.
		/// NOTE: It would probably be better to take a function that returns the script so script generation can be deferred until all controls have client IDs.
		/// </summary>
		public CustomButton( string script ) {
			ActionControlStyle = new ButtonActionControlStyle( "" );
			this.script = script;
		}

		void ControlTreeDataLoader.LoadData( DBConnection cn ) {
			if( TagKey == HtmlTextWriterTag.Button )
				PostBackButton.AddButtonAttributes( this );
			this.AddJavaScriptEventScript( JsWritingMethods.onclick, script );
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