using System.Collections.Generic;

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
		/// <param name="activationBehavior">The activation behavior.</param>
		public EwfTableField(
			IEnumerable<string> classes = null, CssLength size = null, TextAlignment textAlignment = TextAlignment.NotSpecified,
			TableCellVerticalAlignment verticalAlignment = TableCellVerticalAlignment.NotSpecified, ElementActivationBehavior activationBehavior = null ) {
			FieldOrItemSetup = new EwfTableFieldOrItemSetup( classes, size, textAlignment, verticalAlignment, activationBehavior );
		}
	}
}