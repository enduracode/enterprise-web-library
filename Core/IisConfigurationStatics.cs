using Microsoft.Web.Administration;

namespace EnterpriseWebLibrary;

internal static class IisConfigurationStatics {
	internal static void ExecuteInServerManagerTransaction( Action<ServerManager> method ) {
		using var serverManager = new ServerManager();
		method( serverManager );
		serverManager.CommitChanges();
	}
}