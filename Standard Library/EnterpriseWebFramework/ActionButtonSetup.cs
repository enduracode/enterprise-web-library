using System;
using System.Web.UI;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Represents a button that appears in the EWF user interface that performs an action or simply navigates to a URL when clicked.
	/// </summary>
	public class ActionButtonSetup {
		/// <summary>
		/// NOTE: This method will be deleted when RSIS Goal 925 is completed. But continue using it when necessary since there is no good alternative.
		/// </summary>
		public static ActionButtonSetup CreateWithUrl( string text, ResourceInfo resourceInfo ) {
			return new ActionButtonSetup( text, new EwfLink( resourceInfo ) );
		}

		private readonly ActionControl actionControl;
		private readonly string text;

		/// <summary>
		/// Creates an action button with the given behavior (ActionControl). The ActionControlStyle of the given actionControl will be overwritten.
		/// </summary>
		public ActionButtonSetup( string text, ActionControl actionControl ) {
			this.text = text;
			this.actionControl = actionControl;
		}

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		/// <param name="styleSelector">Passes the text of this ActionButtonSetup, returns an ActionControlStyle</param>
		/// <param name="usesSubmitBehavior"></param>
		/// <returns></returns>
		public Control BuildButton( Func<string, ActionControlStyle> styleSelector, bool usesSubmitBehavior ) {
			actionControl.ActionControlStyle = styleSelector( text );

			// NOTE: These control-type-specific blocks suck. We're basically admitting we have this factored wrong (still).
			var asPostBackButton = actionControl as PostBackButton;
			if( asPostBackButton != null )
				asPostBackButton.UsesSubmitBehavior = usesSubmitBehavior;

			return (Control)actionControl;
		}
	}
}