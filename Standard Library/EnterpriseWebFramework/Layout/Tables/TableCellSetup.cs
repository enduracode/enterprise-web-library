using System.Collections.Generic;
using System.Web.UI;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The configuration for a table cell.
	/// </summary>
	public class TableCellSetup {
		internal readonly int FieldSpan;
		internal readonly int ItemSpan;
		internal readonly IEnumerable<string> Classes;
		internal readonly TextAlignment TextAlignment;
		internal readonly ClickScript ClickScript;
		internal readonly string ToolTip;
		internal readonly Control ToolTipControl;

		/// <summary>
		/// Creates a cell setup object.
		/// </summary>
		/// <param name="fieldSpan">The number of fields this cell will span.
		/// NOTE: Don't allow this to be less than one. Zero is allowed by the HTML spec but is too difficult for us to implement right now.
		/// </param>
		/// <param name="itemSpan">The number of items this cell will span.
		/// NOTE: Don't allow this to be less than one. Zero is allowed by the HTML spec but is too difficult for us to implement right now.
		/// </param>
		/// <param name="classes">The CSS class(es).</param>
		/// <param name="textAlignment">The text alignment of the cell.</param>
		/// <param name="clickScript">The click script.</param>
		/// <param name="toolTip">The tool tip to display. Setting ToolTipControl will ignore this property. Do not pass null.</param>
		/// <param name="toolTipControl">The control to display inside the tool tip. This will ignore the ToolTip property.</param>
		public TableCellSetup(
			int fieldSpan = 1, int itemSpan = 1, IEnumerable<string> classes = null, TextAlignment textAlignment = TextAlignment.NotSpecified,
			ClickScript clickScript = null, string toolTip = "", Control toolTipControl = null ) {
			FieldSpan = fieldSpan;
			ItemSpan = itemSpan;
			Classes = classes ?? new string[ 0 ];
			TextAlignment = textAlignment;
			ClickScript = clickScript;
			ToolTip = toolTip;
			ToolTipControl = toolTipControl;
		}
	}
}