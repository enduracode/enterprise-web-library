namespace EnterpriseWebLibrary.ExternalFunctionality;

/// <summary>
/// External PDF logic.
/// </summary>
public interface ExternalPdfProvider {
	/// <summary>
	/// Initializes the provider.
	/// </summary>
	void InitStatics( string asposePdfLicensePath );
}