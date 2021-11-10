using System;

namespace EnterpriseWebLibrary.UserManagement.IdentityProviders {
	public class SamlIdentityProvider: IdentityProvider {
		internal readonly Func<string> CertificateGetter;
		private readonly Action<string> certificateUpdater;

		public SamlIdentityProvider( Func<string> certificateGetter, Action<string> certificateUpdater ) {
			CertificateGetter = certificateGetter;
			this.certificateUpdater = certificateUpdater;
		}

		internal void UpdateCertificate( string certificate ) {
			certificateUpdater( certificate );
		}
	}
}