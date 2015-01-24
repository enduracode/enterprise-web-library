namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A limit on the number of data rows displayed in a dynamic table.
	/// </summary>
	public enum DataRowLimit {
		/// <summary>
		/// Show a max of 50 results.
		/// </summary>
		Fifty = 50,

		/// <summary>
		/// Show a max of five hundred results.
		/// </summary>
		FiveHundred = 500,

		/// <summary>
		/// Show all results.
		/// </summary>
		Unlimited = int.MaxValue
	}
}