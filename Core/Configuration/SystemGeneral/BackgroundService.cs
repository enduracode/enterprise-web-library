namespace EnterpriseWebLibrary.Configuration.SystemGeneral;

/// <summary>
/// A background service.
/// </summary>
public class BackgroundService {
	private readonly SystemGeneralConfigurationService element;
	private readonly string installationFullShortName;

	internal BackgroundService( SystemGeneralConfigurationService element, string installationFullShortName ) {
		this.element = element;
		this.installationFullShortName = installationFullShortName;
	}

	/// <summary>
	/// Gets the name of the service.
	/// </summary>
	public string Name => element.Name;

	/// <summary>
	/// Gets the name of the service assembly.
	/// </summary>
	public string NamespaceAndAssemblyName => element.NamespaceAndAssemblyName;

	/// <summary>
	/// Gets the installed name of the service.
	/// </summary>
	public string InstalledName => installationFullShortName + " - " + element.Name;
}