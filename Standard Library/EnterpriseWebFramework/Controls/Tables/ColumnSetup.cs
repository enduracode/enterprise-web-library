using System.Web.UI.WebControls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// Defines a column in an EWF dynamic table.
	/// </summary>
	public class ColumnSetup {
		/// <summary>
		/// Gets or sets whether or not cells in this column are header cells.
		/// </summary>
		public bool IsHeader { get; set; }

		/// <summary>
		/// Gets or sets the width of this column.
		/// </summary>
		public Unit Width { get; set; }

		/// <summary>
		/// Gets or sets the CSS class on cells in this column.
		/// </summary>
		public string CssClassOnAllCells { get; set; }

		/// <summary>
		/// Gets or sets the tool tip on cells in this column.
		/// </summary>
		public string ToolTipOnCells { get; set; }

		/// <summary>
		/// Creates a column setup object. Options specified on individual cells take precedence over equivalent options specified here.
		/// </summary>
		public ColumnSetup() {
			IsHeader = false;
			Width = Unit.Empty;
			CssClassOnAllCells = "";
			ToolTipOnCells = "";
		}
	}
}