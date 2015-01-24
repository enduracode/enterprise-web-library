using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// Defines a row in an EWF dynamic table. Options specified on individual cells take precedence over equivalent options specified here.
	/// You cannot pass the same row setup object to multiple rows in the same table or to rows in different tables.
	/// </summary>
	public class RowSetup {
		private string cssClass = "";
		
		/// <summary>
		/// Set when table row is drawn.
		/// </summary>
		internal TableRow UnderlyingTableRow { get; set; }

		/// <summary>
		/// Gets or sets whether the row is a header row.
		/// </summary>
		public bool IsHeader { get; set; }

		/// <summary>
		/// Gets or sets the CSS class for the row.
		/// If null is passed, it will be converted to the empty string.
		/// </summary>
		public string CssClass { get { return cssClass; } set { cssClass = value ?? ""; } }

		/// <summary>
		/// Gets or sets the click script for the row.
		/// </summary>
		public ClickScript ClickScript { get; set; }

		/// <summary>
		/// EWF ToolTip to display on this control. Setting ToolTipControl will ignore this property.
		/// </summary>
		public string ToolTip { get; set; }

		/// <summary>
		/// Control to display inside the tool tip. Do not pass null. This will ignore the ToolTip property.
		/// </summary>
		public Control ToolTipControl { get; set; }

		// NOTE: This would probably work better as a call back, since it will always be a waste of time to build the string list when drawing the table.
		/// <summary>
		/// Gets or sets the contents of the row in CSV format.
		/// </summary>
		public List<string> CsvLine { get; set; }

		/// <summary>
		/// Uniquely identifies this row.  Actions can use this to determine what row to act on when they are invoked.
		/// </summary>
		public object UniqueIdentifier { get; set; }

		/// <summary>
		/// Gets or sets the rank ID for this row. Swapping will be enabled for all rows that have a non null rank ID.
		/// Setting this on at least one row of a table adds a column on the right of the table containing controls to move each item up or down the list.  This
		/// consumes 10% of the table width.
		/// </summary>
		public int? RankId { get; set; }
	}
}