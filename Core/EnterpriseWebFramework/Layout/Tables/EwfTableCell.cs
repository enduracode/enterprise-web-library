using System.Collections.Generic;

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
		/// Creates a table-cell collection containing only the cell representing this string.
		/// </summary>
		/// <param name="text"></param>
		public static IReadOnlyCollection<EwfTableCell> ToCellCollection( this string text ) => ( (EwfTableCell)text ).ToCollection();

		/// <summary>
		/// Creates a table cell containing these components.
		/// </summary>
		/// <param name="content"></param>
		/// <param name="setup"></param>
		public static EwfTableCell ToCell( this IReadOnlyCollection<FlowComponent> content, TableCellSetup setup = null ) =>
			new EwfTableCell( setup ?? new TableCellSetup(), content );

		/// <summary>
		/// Creates a table cell containing this component.
		/// </summary>
		/// <param name="content"></param>
		/// <param name="setup"></param>
		public static EwfTableCell ToCell( this FlowComponent content, TableCellSetup setup = null ) => content.ToCollection().ToCell( setup: setup );

		/// <summary>
		/// Creates a table cell representing this string. If you don’t need to pass a setup object, don’t use this method; strings are implicitly converted to
		/// table cells.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="setup"></param>
		public static EwfTableCell ToCell( this string text, TableCellSetup setup ) =>
			new EwfTableCell( setup, ( text ?? "" ).ToComponents(), simpleText: text ?? "" );
	}
}