﻿namespace EnterpriseWebLibrary.ExternalFunctionality {
	/// <summary>
	/// System-specific logic for external functionality.
	/// </summary>
	public abstract class SystemExternalFunctionalityProvider {
		/// <summary>
		/// Returns the external SAML provider for the system. This should be a simple constructor expression with no other logic.
		/// </summary>
		protected internal virtual ExternalSamlProvider GetSamlProvider() => null;
	}
}