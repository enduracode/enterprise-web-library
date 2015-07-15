namespace EnterpriseWebLibrary.Configuration.SystemGeneral {
	/// <summary>
	/// A Windows service.
	/// </summary>
	public class WindowsService {
		private readonly SystemGeneralConfigurationService element;
		private readonly string installationFullShortName;

		internal WindowsService( SystemGeneralConfigurationService element, string installationFullShortName ) {
			this.element = element;
			this.installationFullShortName = installationFullShortName;
		}

		/// <summary>
		/// Gets the name of the service.
		/// </summary>
		public string Name { get { return element.Name; } }

		/// <summary>
		/// Gets the name of the service assembly.
		/// </summary>
		public string NamespaceAndAssemblyName { get { return element.NamespaceAndAssemblyName; } }

		/// <summary>
		/// Gets the installed name of the service.
		/// </summary>
		public string InstalledName { get { return installationFullShortName + " - " + element.Name; } }
	}
}