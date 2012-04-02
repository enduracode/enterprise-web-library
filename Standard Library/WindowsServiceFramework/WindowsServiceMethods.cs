using System.Linq;
using System.ServiceProcess;

namespace RedStapler.StandardLibrary.WindowsServiceFramework {
	/// <summary>
	/// A collection of service-related static methods.
	/// </summary>
	public static class WindowsServiceMethods {
		/// <summary>
		/// Creates a service process installer.
		/// </summary>
		public static ServiceProcessInstaller CreateServiceProcessInstaller() {
			return new ServiceProcessInstaller { Account = ServiceAccount.NetworkService };
		}

		/// <summary>
		/// Creates a service installer for the specified service.
		/// </summary>
		public static ServiceInstaller CreateServiceInstaller( WindowsServiceBase service ) {
			return new ServiceInstaller { ServiceName = GetServiceInstalledName( service ), Description = service.Description, StartType = ServiceStartMode.Automatic };
		}

		internal static string GetServiceInstalledName( WindowsServiceBase service ) {
			return AppTools.InstallationConfiguration.WindowsServices.Single( s => s.Name == service.Name ).InstalledName;
		}
	}
}