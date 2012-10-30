using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayElements;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Ui {
	/// <summary>
	/// Application-specific configuration for the EWF UI.
	/// </summary>
	public abstract class AppEwfUiProvider {
		/// <summary>
		/// Gets the logo control to be shown at the top of the EWF user interface.
		/// </summary>
		public virtual WebControl GetLogoControl() {
			return null;
		}

		/// <summary>
		/// Gets the global nav action controls.
		/// </summary>
		public virtual List<ActionButtonSetup> GetGlobalNavActionControls() {
			return new List<ActionButtonSetup>();
		}

		/// <summary>
		/// Gets the global nav lookup box setups.
		/// </summary>
		public virtual List<LookupBoxSetup> GetGlobalNavLookupBoxSetups() {
			return new List<LookupBoxSetup>();
		}

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
	}
}