using System;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Ui {
	/// <summary>
	/// Represents a textbox that appears in the navigation bar allowing users to lookup a record by number.
	/// </summary>
	public class LookupBoxSetup {
		private readonly int pixelWidth;
		private readonly string defaultText;
		private readonly PageInfo autoCompleteService;
		private readonly string postBackId;
		private readonly Func<string, ResourceInfo> handler;

		/// <summary>
		/// Creates a lookup box.
		/// </summary>
		/// <param name="pixelWidth"></param>
		/// <param name="defaultText">Text displayed when the LookupBox does not have focus. Do not pass null.</param>
		/// <param name="postBackId"></param>
		/// <param name="handler">Supplies the string entered into the LookupBox from the user. Returns the resource the user will be redirected to.</param>
		/// <param name="autoCompleteService"></param>
		public LookupBoxSetup( int pixelWidth, string defaultText, string postBackId, Func<string, ResourceInfo> handler, PageInfo autoCompleteService = null ) {
			this.pixelWidth = pixelWidth;
			this.defaultText = defaultText;
			this.autoCompleteService = autoCompleteService;
			this.postBackId = postBackId;
			this.handler = handler;
		}

		/// <summary>
		/// Builds this LookupBox and returns the panel.
		/// </summary>
		public WebControl BuildLookupBoxPanel() {
			var val = new DataValue<string>();
			var postBack = PostBack.CreateFull( id: postBackId, actionGetter: () => new PostBackAction( handler( val.Value ) ) );
			return FormState.ExecuteWithDataModificationsAndDefaultAction(
				postBack.ToCollection(),
				() => new Block(
					val.ToTextControl(
							true,
							setup: autoCompleteService != null
								       ? TextControlSetup.CreateAutoComplete( autoCompleteService, placeholder: defaultText, triggersActionWhenItemSelected: true )
								       : TextControlSetup.Create( placeholder: defaultText ),
							value: "" )
						.ToFormItem()
						.ToControl() ) { CssClass = "ewfLookupBox", Width = Unit.Pixel( pixelWidth ) } );
		}
	}
}