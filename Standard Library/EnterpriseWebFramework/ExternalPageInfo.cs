namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A web resource outside of the system.
	/// </summary>
	public sealed class ExternalPageInfo: PageInfo {
		private readonly string url;
		private readonly string name;

		/// <summary>
		/// Creates an ExternalPageInfo. Do not pass null or the empty string for url. Do not pass null for uriFragmentIdentifier or name.
		/// </summary>
		public ExternalPageInfo( string url, string uriFragmentIdentifier = "", string name = "" ) {
			this.url = url;
			base.uriFragmentIdentifier = uriFragmentIdentifier;
			this.name = name;
		}

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public override EntitySetupInfo EsInfoAsBaseType { get { return null; } }

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public override string PageName { get { return name; } }

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		protected override string buildUrl() {
			return url;
		}

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		protected override bool isIdenticalTo( PageInfo infoAsBaseType ) {
			var info = infoAsBaseType as ExternalPageInfo;
			return info != null && info.url == url && info.uriFragmentIdentifier == uriFragmentIdentifier && info.name == name;
		}

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		protected internal override PageInfo CloneAndReplaceDefaultsIfPossible( bool disableReplacementOfDefaults ) {
			return new ExternalPageInfo( url, uriFragmentIdentifier: uriFragmentIdentifier, name: name );
		}
	}
}