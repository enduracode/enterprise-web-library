namespace EnterpriseWebLibrary.Configuration;

/// <summary>
/// EWL use only.
/// </summary>
public class SystemProviderReference<ProviderType> where ProviderType: class {
	private readonly ProviderType? provider;
	private readonly string errorMessage;

	internal SystemProviderReference( ProviderType? provider, string errorMessage ) {
		this.provider = provider;
		this.errorMessage = errorMessage;
	}

	public ProviderType? GetProvider( bool returnNullIfNotFound = false ) =>
		provider != null || returnNullIfNotFound ? provider : throw new ApplicationException( errorMessage );
}