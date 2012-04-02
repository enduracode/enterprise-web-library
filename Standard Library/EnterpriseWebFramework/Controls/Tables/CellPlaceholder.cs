namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// Represents either a cell or the space taken up be a spanning cell.
	/// </summary>
	internal interface CellPlaceholder {
		/// <summary>
		/// Returns null if the cell does not contain simple text (such as a Control).
		/// </summary>
		string SimpleText { get; }
	}
}