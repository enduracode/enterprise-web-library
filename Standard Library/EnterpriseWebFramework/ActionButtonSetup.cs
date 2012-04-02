using System;
using System.Web.UI;
using RedStapler.StandardLibrary.EnterpriseWebFramework.AlternativePageModes;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Represents a button that appears in the EWF user interface that performs an action or simply navigates to a URL when clicked.
	/// </summary>
	public class ActionButtonSetup {
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
		/// Creates an action button setup that will create an action button that navigates to a URL when clicked.
		/// NOTE: This method will be deleted. Do not use.
		/// </summary>
		public static ActionButtonSetup CreateWithUrl( string text, PageInfo pageInfo ) {
			return new ActionButtonSetup( text, new EwfLink( pageInfo ) );
		}

		/// <summary>
		/// Creates an action button setup that will create an action button that executes code when clicked.
		/// NOTE: This method will be deleted. Do not use.
		/// </summary>
		public static ActionButtonSetup CreateWithAction( string text, Action action ) {
			return new ActionButtonSetup( text, new PostBackButton( new DataModification(), action ) );
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

			var asEwfLink = actionControl as EwfLink;
			// NOTE: I would concatenate the style here, but that admits that they could have added styles to the control, which we don't want them to do. Ugh.
			if( asEwfLink != null && asEwfLink.NavigatePageInfo != null && asEwfLink.NavigatePageInfo.AlternativeMode is NewContentPageMode )
				asEwfLink.CssClass = "ewfNewness";

			var asPostBackButton = actionControl as PostBackButton;
			if( asPostBackButton != null )
				asPostBackButton.UsesSubmitBehavior = usesSubmitBehavior;


			return (Control)actionControl;
		}
	}
}