using System.Collections.Generic;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A cell in a table.
	/// </summary>
	public class EwfTableCell: CellPlaceholder {
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
		/// Creates a table cell containing components that represent this string.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="setup"></param>
		public static EwfTableCell ToCell( this string text, TableCellSetup setup = null ) =>
			new EwfTableCell( setup ?? new TableCellSetup(), ( text ?? "" ).ToComponents(), simpleText: text ?? "" );

		/// <summary>
		/// Concatenates table cells.
		/// </summary>
		public static IEnumerable<EwfTableCell> Concat( this EwfTableCell first, IEnumerable<EwfTableCell> second ) => second.Prepend( first );

		/// <summary>
		/// Returns a sequence of two table cells.
		/// </summary>
		public static IEnumerable<EwfTableCell> Append( this EwfTableCell first, EwfTableCell second ) =>
			Enumerable.Empty<EwfTableCell>().Append( first ).Append( second );
	}
}