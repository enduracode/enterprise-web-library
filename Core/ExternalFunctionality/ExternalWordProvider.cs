namespace EnterpriseWebLibrary.ExternalFunctionality;

/// <summary>
/// External Word document logic.
/// </summary>
public interface ExternalWordProvider {
	/// <summary>
	/// Initializes the provider.
	/// </summary>
	void InitStatics( string asposeWordsLicensePath );
}