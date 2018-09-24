using System.Collections.Generic;
using System.Linq;
using System.Web.UI;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Application-specific configuration for the EWF UI.
	/// </summary>
	public abstract class AppEwfUiProvider {
		/// <summary>
		/// Creates and returns a list of custom style sheets that should be used on EWF UI pages.
		/// </summary>
		public virtual List<ResourceInfo> GetStyleSheets() {
			return new List<ResourceInfo>();
		}

		/// <summary>
		/// Gets the logo to be shown at the top of the EWF user interface. Returns null if the application display name should be used instead.
		/// </summary>
		public virtual IReadOnlyCollection<FlowComponent> GetLogoComponent() {
			return null;
		}

		/// <summary>
		/// Gets the global navigational action components.
		/// </summary>
		public virtual IReadOnlyCollection<ActionComponentSetup> GetGlobalNavActions() => Enumerable.Empty<ActionComponentSetup>().Materialize();

		/// <summary>
		/// Gets the global navigational form controls.
		/// </summary>
		public virtual IReadOnlyCollection<NavFormControl> GetGlobalNavFormControls() => Enumerable.Empty<NavFormControl>().Materialize();

		/// <summary>
		/// Gets whether items in the global nav control list are separated with a pipe character.
		/// </summary>
		public virtual bool GlobalNavItemsSeparatedWithPipe() {
			return true;
		}

		/// <summary>
		/// Gets whether items in the entity nav and entity action control lists are separated with a pipe character.
		/// </summary>
		public virtual bool EntityNavAndActionItemsSeparatedWithPipe() {
			return true;
		}

		/// <summary>
		/// Gets whether items in the page action control list are separated with a pipe character.
		/// </summary>
		public virtual bool PageActionItemsSeparatedWithPipe() {
			return true;
		}

		/// <summary>
		/// Gets whether the browser warning is disabled.
		/// </summary>
		public virtual bool BrowserWarningDisabled() {
			return false;
		}

		/// <summary>
		/// Gets the control to be shown at the bottom of the log in page for systems with forms authentication.
		/// </summary>
		public virtual Control GetSpecialInstructionsForLogInPage() {
			return null;
		}

		/// <summary>
		/// Gets the global foot controls.
		/// </summary>
		public virtual IEnumerable<Control> GetGlobalFootControls() {
			return new Control[ 0 ];
		}

		/// <summary>
		/// Gets whether the "Powered by the Enterprise Web Library" footer is disabled.
		/// </summary>
		public virtual bool PoweredByEwlFooterDisabled() {
			return false;
		}
	}
}