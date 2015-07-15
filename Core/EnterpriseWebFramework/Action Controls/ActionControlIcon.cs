using System;
using System.Collections.Generic;
using System.Web.UI;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public class ActionControlIcon {
		internal static IEnumerable<Control> GetIconAndTextControls( ActionControlIcon icon, string text ) {
			// Use a label because our CSS selectors for icons include first-child and last-child and these do not take into account "text nodes", i.e. text that is
			// interspersed with elements.
			var textControl = text.GetLabelControl();

			if( icon == null )
				return textControl.ToSingleElementArray();
			if( icon.placement == ActionControlIconPlacement.Left )
				return new Control[] { icon.icon, textControl };
			if( icon.placement == ActionControlIconPlacement.Right )
				return new Control[] { textControl, icon.icon };
			throw new ApplicationException( "unknown placement" );
		}

		private readonly ActionControlIconPlacement placement;
		private readonly FontAwesomeIcon icon;

		/// <summary>
		/// Creates an action control icon.
		/// </summary>
		/// <param name="icon">The icon. Do not pass null.</param>
		/// <param name="placement">The placement of the icon. We recommend the left side in most cases; see
		/// http://uxmovement.com/buttons/where-to-place-icons-next-to-button-labels/.</param>
		public ActionControlIcon( FontAwesomeIcon icon, ActionControlIconPlacement placement = ActionControlIconPlacement.Left ) {
			this.placement = placement;
			this.icon = icon;
		}
	}
}