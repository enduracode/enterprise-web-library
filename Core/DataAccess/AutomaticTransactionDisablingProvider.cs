using System.Collections.Generic;

namespace EnterpriseWebLibrary.DataAccess {
	/// <summary>
	/// System-specific automatic-transaction disabling logic.
	/// </summary>
	public interface AutomaticTransactionDisablingProvider: SystemDataAccessProvider {
		/// <summary>
		/// Returns the names of secondary databases that should have automatic transactions disabled.
		/// </summary>
		IEnumerable<string> GetDisabledAutomaticTransactionSecondaryDatabaseNames();
	}
}