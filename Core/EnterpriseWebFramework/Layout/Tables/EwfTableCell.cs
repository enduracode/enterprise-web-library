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
			Controls = ( text ?? "" ).GetLiteralControl().ToCollection();
			simpleText = text;
		}

		internal EwfTableCell( TableCellSetup setup, Control control ): this( setup, control != null ? control.ToCollection() : new Control[ 0 ] ) {}

		internal EwfTableCell( TableCellSetup setup, IEnumerable<Control> controls ) {
			Setup = setup;

			Controls = controls.ToImmutableArray();
			Controls = Controls.Any() ? Controls : "".GetLiteralControl().ToCollection();

			simpleText = null;
		}

		string CellPlaceholder.SimpleText => simpleText;
	}
}