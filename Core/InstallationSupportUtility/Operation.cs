using System.Collections.Generic;
using EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;

namespace EnterpriseWebLibrary.InstallationSupportUtility {
	public interface Operation {
		bool IsValid( Installation installation );

		/// <summary>
		/// Never call this method directly.
		/// </summary>
		void Execute( Installation installation, IReadOnlyList<string> arguments, OperationResult operationResult );
	}
}