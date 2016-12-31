using System.Collections.Generic;
using System.Web.UI;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A display style for modification errors.
	/// </summary>
	public interface ErrorDisplayStyle {
		/// <summary>
		/// EWL use only.
		/// </summary>
		IEnumerable<Control> GetControls( IEnumerable<string> errors );
	}
}