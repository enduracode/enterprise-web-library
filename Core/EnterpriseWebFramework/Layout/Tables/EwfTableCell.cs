using System.Collections.Generic;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A cell in a table.
	/// </summary>
	public class EwfTableCell: CellPlaceholder {
		public static implicit operator EwfTableCell( string text ) => text.ToCell( new TableCellSetup() );

		internal readonly TableCellSetup Setup;
		internal readonly IReadOnlyCollection<FlowComponent> Content;
		private readonly string simpleText;

		internal EwfTableCell( TableCellSetup setup, IReadOnlyCollection<FlowComponent> content, string simpleText = null ) {
			Setup = setup;
			Content = content;
			this.simpleText = simpleText;
		}

		string CellPlaceholder.SimpleText => simpleText;
	}

	public static class EwfTableCellExtensionCreators {
		/// <summary>
		/// Creates a table cell containing these components.
		/// </summary>
		public static EwfTableCell ToCell( this IReadOnlyCollection<FlowComponent> content, TableCellSetup setup = null ) =>
			new EwfTableCell( setup ?? new TableCellSetup(), content );

		/// <summary>
		/// Creates a table cell containing this string. If you don’t need to pass a setup object, don’t use this method; strings are implicitly converted to table
		/// cells.
		/// </summary>
		public static EwfTableCell ToCell( this string text, TableCellSetup setup ) =>
			new EwfTableCell( setup, ( text ?? "" ).ToComponents(), simpleText: text ?? "" );
	}
}