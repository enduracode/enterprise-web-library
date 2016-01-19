using System.Collections.Generic;
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
		internal readonly IEnumerable<Control> Controls;
		private readonly string simpleText;

		internal EwfTableCell( TableCellSetup setup, string text ) {
			Setup = setup;
			Controls = ( text ?? "" ).GetLiteralControl().ToSingleElementArray();
			simpleText = text;
		}

		internal EwfTableCell( TableCellSetup setup, Control control ): this( setup, control != null ? control.ToSingleElementArray() : new Control[ 0 ] ) {}

		internal EwfTableCell( TableCellSetup setup, IEnumerable<Control> controls ) {
			Setup = setup;
			Controls = controls.Any() ? controls : "".GetLiteralControl().ToSingleElementArray();
			simpleText = null;
		}

		string CellPlaceholder.SimpleText { get { return simpleText; } }
	}
}