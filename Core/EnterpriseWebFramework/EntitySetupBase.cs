namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A control that allows several EWF pages to share query parameters, authorization logic, data, etc.
	/// </summary>
	public interface EntitySetupBase {
		/// <summary>
		/// Gets the entity setup info object for this entity setup.
		/// </summary>
		EntitySetupInfo InfoAsBaseType { get; }

		/// <summary>
		/// Gets the parameters modification object for this entity setup.
		/// </summary>
		ParametersModificationBase ParametersModificationAsBaseType { get; }

		/// <summary>
		/// Creates the info object for this entity setup based on the query parameters of the request.
		/// </summary>
		void CreateInfoFromQueryString();

		/// <summary>
		/// Loads data needed by the entity setup and by all pages that use the entity setup. This is called prior to the page's LoadData method.
		/// </summary>
		void LoadData();
	}
}