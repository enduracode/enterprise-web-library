using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	///  A field in a table. Options specified on individual cells take precedence over equivalent options specified here.
	/// </summary>
	public class EwfTableField {
		internal readonly EwfTableFieldOrItemSetup FieldOrItemSetup;

		/// <summary>
		/// Creates a table field.
		/// </summary>
		/// <param name="classes">The CSS class(es). When used on a column, sets the class on every cell since most styles don't work on col elements.</param>
		/// <param name="size">The height or width. For an EWF table, this is the column width. For a column primary table, this is the row height. If you specify
		/// percentage widths for some or all columns in a table, these values need not add up to 100; they will be automatically scaled if necessary. The automatic
		/// scaling will not happen if there are any columns without a specified width.</param>
		/// <param name="textAlignment">The text alignment of the cells in this field.</param>
		/// <param name="verticalAlignment">The vertical alignment of the cells in this field.</param>
		/// <param name="clickScript">The click script.</param>
		/// <param name="toolTip">The tool tip to display. Setting ToolTipControl will ignore this property. Do not pass null.</param>
		/// <param name="toolTipControl">The control to display inside the tool tip. This will ignore the ToolTip property.</param>
		public EwfTableField( IEnumerable<string> classes = null, Unit? size = null, TextAlignment textAlignment = TextAlignment.NotSpecified,
		                      TableCellVerticalAlignment verticalAlignment = TableCellVerticalAlignment.NotSpecified, ClickScript clickScript = null,
		                      string toolTip = "", Control toolTipControl = null ) {
			FieldOrItemSetup = new EwfTableFieldOrItemSetup( classes, size, textAlignment, verticalAlignment, clickScript, toolTip, toolTipControl );
		}
	}
}