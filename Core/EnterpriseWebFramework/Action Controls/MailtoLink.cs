using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// Creates a special mailto link that upon a user's click, will open their mail client with the designated fields filled.
	/// </summary>
	public class MailtoLink: WebControl, ControlTreeDataLoader, ControlWithJsInitLogic, ActionControl {
		/// <summary>
		/// Sets the display style of this button. Do not set this to null.
		/// Choices are: ButtonActionControlStyle, BoxActionControlStyle, ImageActionControlStyle, CustomActionControlStyle, and TextActionControlStyle (default).
		/// </summary>
		public ActionControlStyle ActionControlStyle { private get; set; }

		/// <summary>
		/// Address to appear in the To: field.
		/// </summary>
		public string ToAddress { get; set; }

		/// <summary>
		/// Address to appear in the CC: field.
		/// </summary>
		public string CcAddress { get; set; }

		/// <summary>
		/// Address to appear in the BCC: field.
		/// </summary>
		public string BccAddress { get; set; }

		/// <summary>
		/// Text to appear in the subject field.
		/// </summary>
		public string Subject { get; set; }

		/// <summary>
		/// Message to appear in the body.
		/// </summary>
		public string Body { get; set; }

		/// <summary>
		/// EWF ToolTip to display on this control. Setting ToolTipControl will ignore this property.
		/// </summary>
		public override string ToolTip { get; set; }

		/// <summary>
		/// Control to display inside the tool tip. Do not pass null. This will ignore the ToolTip property.
		/// </summary>
		public Control ToolTipControl { get; set; }

		/// <summary>
		/// Creates a new Mailto Link.
		/// </summary>
		public MailtoLink() {
			ActionControlStyle = new TextActionControlStyle( "" );
			ToAddress = "%20";
			CcAddress = "";
			BccAddress = "";
			Subject = "";
			Body = "";
		}

		void ControlTreeDataLoader.LoadData() {
			Attributes.Add(
				"href",
				"mailto:" +
				StringTools.ConcatenateWithDelimiter(
					"?",
					ToAddress,
					StringTools.ConcatenateWithDelimiter(
						"&",
						CcAddress.PrependDelimiter( "cc=" ),
						BccAddress.PrependDelimiter( "bcc=" ),
						HttpUtility.UrlEncode( Subject ).PrependDelimiter( "subject=" ),
						HttpUtility.UrlEncode( Body ).PrependDelimiter( "body=" ) ) ) );

			CssClass = CssClass.ConcatenateWithSpace( "ewfClickable" );
			ActionControlStyle.SetUpControl( this, "" );

			if( ToolTip != null || ToolTipControl != null )
				new ToolTip( ToolTipControl ?? EnterpriseWebFramework.Controls.ToolTip.GetToolTipTextControl( ToolTip ), this );
		}

		string ControlWithJsInitLogic.GetJsInitStatements() {
			return ActionControlStyle.GetJsInitStatements();
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey => HtmlTextWriterTag.A;
	}
}