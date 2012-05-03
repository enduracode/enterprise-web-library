using RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel;

namespace RedStapler.StandardLibrary.InstallationSupportUtility {
	public interface Operation {
		bool IsValid( Installation installation );

		/// <summary>
		/// Never call this method directly.
		/// </summary>
		void Execute( Installation installation, OperationResult operationResult );
	}
}