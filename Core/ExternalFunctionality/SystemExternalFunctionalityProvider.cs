namespace EnterpriseWebLibrary.ExternalFunctionality;

/// <summary>
/// System-specific logic for external functionality.
/// </summary>
public abstract class SystemExternalFunctionalityProvider {
	/// <summary>
	/// Returns the external MySQL provider for the system. This should be a simple constructor expression with no other logic.
	/// </summary>
	protected internal virtual ExternalMySqlProvider? GetMySqlProvider() => null;

	/// <summary>
	/// Returns the external Oracle Database provider for the system. This should be a simple constructor expression with no other logic.
	/// </summary>
	protected internal virtual ExternalOracleDatabaseProvider? GetOracleDatabaseProvider() => null;

	/// <summary>
	/// Returns the external SQLite provider for the system. This should be a simple constructor expression with no other logic.
	/// </summary>
	protected internal virtual ExternalSqliteProvider? GetSqliteProvider() => null;

	/// <summary>
	/// Returns the external OpenID Connect provider for the system. This should be a simple constructor expression with no other logic.
	/// </summary>
	protected internal virtual ExternalOpenIdConnectProvider? GetOpenIdConnectProvider() => null;

	/// <summary>
	/// Returns the external SAML provider for the system. This should be a simple constructor expression with no other logic.
	/// </summary>
	protected internal virtual ExternalSamlProvider? GetSamlProvider() => null;

	/// <summary>
	/// Returns the external PDF provider for the system. This should be a simple constructor expression with no other logic.
	/// </summary>
	protected internal virtual ExternalPdfProvider? GetPdfProvider() => null;

	/// <summary>
	/// Returns the external Word provider for the system. This should be a simple constructor expression with no other logic.
	/// </summary>
	protected internal virtual ExternalWordProvider? GetWordProvider() => null;
}