using System;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayElements {
	/// <summary>
	/// Represents a textbox that appears in the navigation bar allowing users to lookup a record by number.
	/// </summary>
	public class LookupBoxSetup {
		private readonly int pixelWidth;
		private readonly string defaultText;
		private readonly Func<DBConnection, string, string> handler;
		private readonly WebMethodDefinition webMethodDefinition;

		/// <summary>
		/// Creates a lookup box.
		/// </summary>
		/// <param name="pixelWidth"></param>
		/// <param name="defaultText">Text displayed when the LookupBox does not have focus.</param>
		/// <param name="handler">Supplies a database connection and the string entered into the LookupBox from the user. Returns the URL the user will be redirected to.</param>
		public LookupBoxSetup( int pixelWidth, string defaultText, Func<DBConnection, string, string> handler ) {
			this.pixelWidth = pixelWidth;
			this.defaultText = defaultText;
			this.handler = handler;
		}

		/// <summary>
		/// Creates a lookup box.
		/// </summary>
		/// <param name="pixelWidth"></param>
		/// <param name="defaultText">Text displayed when the LookupBox does not have focus.</param>
		/// <param name="webMethodDefinition">The method in this WebMethodDefinition must accept (string textPassedFromUser, int count) and return an array of strings.</param>
		/// <param name="handler">Supplies a database connection and the string entered into the LookupBox from the user. Returns the URL the user will be redirected to.</param>
		public LookupBoxSetup( int pixelWidth, string defaultText, Func<DBConnection, string, string> handler, WebMethodDefinition webMethodDefinition )
			: this( pixelWidth, defaultText, handler ) {
			this.webMethodDefinition = webMethodDefinition;
		}

		/// <summary>
		/// Builds this LookupBox and returns the panel.
		/// </summary>
		public Panel BuildLookupBoxPanel() {
			EwfTextBox textBox = null;
			textBox = new EwfTextBox( "", postBackHandler: () => EwfPage.Instance.EhModifyDataAndRedirect( cn => handler( cn, textBox.Value ) ) )
			          	{ Width = new Unit( pixelWidth ) };
			textBox.SetWatermarkText( defaultText );
			if( webMethodDefinition != null )
				textBox.SetupAutoFill( webMethodDefinition, AutoFillOptions.PostBackOnItemSelect );
			return new Panel { CssClass = "ewfLookupBox" }.AddControlsReturnThis( textBox );
		}
	}
}