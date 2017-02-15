using System;
using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public class ActionComponentIcon {
		internal static IReadOnlyCollection<PhrasingComponent> GetIconAndTextComponents( ActionComponentIcon icon, string text ) {
			// Use a container because our CSS selectors for icons include first-child and last-child and these do not take into account "text nodes", i.e. text that
			// is interspersed with elements.
			var textComponent = new GenericPhrasingContainer( text.ToComponent().ToCollection() );

			if( icon == null )
				return textComponent.ToCollection();
			if( icon.placement == ActionComponentIconPlacement.Left )
				return new PhrasingComponent[] { icon.icon, textComponent };
			if( icon.placement == ActionComponentIconPlacement.Right )
				return new PhrasingComponent[] { textComponent, icon.icon };
			throw new ApplicationException( "unknown placement" );
		}

		private readonly ActionComponentIconPlacement placement;
		private readonly FontAwesomeIcon icon;

		/// <summary>
		/// Creates an action-component icon.
		/// </summary>
		/// <param name="icon">The icon. Do not pass null.</param>
		/// <param name="placement">The placement of the icon. We recommend the left side in most cases; see
		/// http://uxmovement.com/buttons/where-to-place-icons-next-to-button-labels/.</param>
		public ActionComponentIcon( FontAwesomeIcon icon, ActionComponentIconPlacement placement = ActionComponentIconPlacement.Left ) {
			this.placement = placement;
			this.icon = icon;
		}
	}
}