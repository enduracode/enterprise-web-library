using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Web.UI;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A cell in a table.
	/// </summary>
	public class EwfTableCell: CellPlaceholder {
		public static implicit operator EwfTableCell( string text ) {
			return new EwfTableCell( new TableCellSetup(), text );
		}

		public static implicit operator EwfTableCell( Control control ) {
			return new EwfTableCell( new TableCellSetup(), control );
		}

		internal readonly TableCellSetup Setup;
		internal readonly IReadOnlyCollection<Control> Controls;
		private readonly string simpleText;

		internal EwfTableCell( TableCellSetup setup, string text ) {
			Setup = setup;
			Controls = ( text ?? "" ).ToComponents().GetControls().ToImmutableArray();
			simpleText = text;
		}

		internal EwfTableCell( TableCellSetup setup, Control control ): this( setup, control != null ? control.ToCollection() : new Control[ 0 ] ) {}

		internal EwfTableCell( TableCellSetup setup, IEnumerable<Control> controls ) {
			Setup = setup;

			Controls = controls.ToImmutableArray();
			Controls = Controls.Any() ? Controls : "".ToComponents().GetControls().ToImmutableArray();

			simpleText = null;
		}

		string CellPlaceholder.SimpleText => simpleText;
	}

	public static class EwfTableCellExtensionCreators {
		/// <summary>
		/// Creates a table cell containing these components.
		/// </summary>
		public static EwfTableCell ToCell( this IEnumerable<FlowComponent> content, TableCellSetup setup = null ) {
			return new EwfTableCell( setup ?? new TableCellSetup(), content.GetControls() );
		}

		/// <summary>
		/// Creates a table cell containing an HTML-encoded version of this string. If the string is empty, the cell will contain a non-breaking space. If you don't
		/// need to pass a setup object, don't use this method; strings are implicitly converted to table cells.
		/// </summary>
		public static EwfTableCell ToCell( this string text, TableCellSetup setup ) {
			return new EwfTableCell( setup, text );
		}

		/// <summary>
		/// Creates a table cell containing this control. If the control is null, the cell will contain a non-breaking space. If you don't need to pass a setup
		/// object, don't use this method; controls are implicitly converted to table cells.
		/// </summary>
		public static EwfTableCell ToCell( this Control control, TableCellSetup setup ) {
			return new EwfTableCell( setup, control );
		}

		/// <summary>
		/// Creates a table cell containing these controls. If no controls exist, the cell will contain a non-breaking space.
		/// </summary>
		public static EwfTableCell ToCell( this IEnumerable<Control> controls, TableCellSetup setup = null ) {
			return new EwfTableCell( setup ?? new TableCellSetup(), controls );
		}
	}
}