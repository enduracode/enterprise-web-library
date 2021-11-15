using System;
using System.Collections.Generic;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.ExternalFunctionality;
using EnterpriseWebLibrary.UserManagement.IdentityProviders;

namespace EnterpriseWebLibrary.Saml {
	public class SamlProvider: ExternalSamlProvider {
		void ExternalSamlProvider.InitStatics() {
			throw new NotImplementedException();
		}

		void ExternalSamlProvider.InitAppStatics( SystemProviderGetter providerGetter ) {
			throw new NotImplementedException();
		}

		void ExternalSamlProvider.InitAppSpecificLogicDependencies( IReadOnlyCollection<SamlIdentityProvider> samlIdentityProviders ) {
			throw new NotImplementedException();
		}
	}
}