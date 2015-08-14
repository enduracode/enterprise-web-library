using System;
using System.Web.UI;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Represents a button that appears in the EWF user interface that performs an action or simply navigates to a URL when clicked.
	/// </summary>
	public class ActionButtonSetup {
		/// <summary>
		/// NOTE: This method will be deleted when RSIS Goal 925 is completed. But continue using it when necessary since there is no good alternative.
		/// </summary>
		public static ActionButtonSetup CreateWithUrl( string text, ResourceInfo resourceInfo, ActionControlIcon icon = null ) {
			return new ActionButtonSetup( text, new EwfLink( resourceInfo ), icon: icon );
		}

		private readonly ActionControl actionControl;
		private readonly string text;
		private readonly ActionControlIcon icon;

		/// <summary>
		/// Creates an action button with the given behavior (ActionControl). The ActionControlStyle of the given actionControl will be overwritten.
		/// </summary>
		public ActionButtonSetup( string text, ActionControl actionControl, ActionControlIcon icon = null ) {
			this.text = text;
			this.icon = icon;
			this.actionControl = actionControl;
		}

		/// <summary>
		/// EWF use only.
		/// </summary>
		public Control BuildButton( Func<string, ActionControlIcon, ActionControlStyle> styleSelector, bool usesSubmitBehavior ) {
			actionControl.ActionControlStyle = styleSelector( text, icon );

			// NOTE: These control-type-specific blocks suck. We're basically admitting we have this factored wrong (still).
			var asPostBackButton = actionControl as PostBackButton;
			if( asPostBackButton != null )
				asPostBackButton.UsesSubmitBehavior = usesSubmitBehavior;

			return (Control)actionControl;
		}
	}
}