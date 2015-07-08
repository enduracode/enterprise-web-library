namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A web resource outside of the system.
	/// </summary>
	public sealed class ExternalResourceInfo: ResourceInfo {
		private readonly string url;
		private readonly string name;

		/// <summary>
		/// Creates an ExternalResourceInfo. Do not pass null or the empty string for url. Do not pass null for uriFragmentIdentifier or name.
		/// </summary>
		public ExternalResourceInfo( string url, string uriFragmentIdentifier = "", string name = "" ) {
			this.url = url;
			base.uriFragmentIdentifier = uriFragmentIdentifier;
			this.name = name;
		}

		/// <summary>
		/// EWL use only.
		/// </summary>
		public override EntitySetupInfo EsInfoAsBaseType { get { return null; } }

		/// <summary>
		/// EWL use only.
		/// </summary>
		public override string ResourceName { get { return name; } }

		/// <summary>
		/// EWL use only.
		/// </summary>
		protected override string buildUrl() {
			return url;
		}

		/// <summary>
		/// EWL use only.
		/// </summary>
		protected override bool isIdenticalTo( ResourceInfo infoAsBaseType ) {
			var info = infoAsBaseType as ExternalResourceInfo;
			return info != null && info.url == url && info.uriFragmentIdentifier == uriFragmentIdentifier && info.name == name;
		}

		/// <summary>
		/// EWL use only.
		/// </summary>
		protected internal override ResourceInfo CloneAndReplaceDefaultsIfPossible( bool disableReplacementOfDefaults ) {
			return new ExternalResourceInfo( url, uriFragmentIdentifier: uriFragmentIdentifier, name: name );
		}
	}
}