using System;
using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A spacer cell setup for a control list.
	/// </summary>
	public class ControlListSpacerCellSetup {
		private readonly Func<List<EwfTableCell>> rowSpacerCellCreator;
		private readonly Func<List<EwfTableCell>> columnSpacerCellCreator;
		private readonly Func<List<List<EwfTableCell>>> rowAndColumnSpacerCellCreator;

		/// <summary>
		/// Creates a control list spacer cell setup.
		/// </summary>
		public ControlListSpacerCellSetup() {
			rowSpacerCellCreator = delegate { return new List<EwfTableCell>(); };
			columnSpacerCellCreator = delegate { return new List<EwfTableCell>(); };
			rowAndColumnSpacerCellCreator = delegate { return new List<List<EwfTableCell>>(); };
		}

		// NOTE: Add a constructor that takes only columnSpacerCells.
		// NOTE: Add a constructor that takes only rowSpacerCells.

		/// <summary>
		/// Creates a control list spacer cell setup. Do not pass null for any parameters.
		/// </summary>
		public ControlListSpacerCellSetup( Func<List<EwfTableCell>> rowSpacerCellCreator, Func<List<EwfTableCell>> columnSpacerCellCreator,
		                                   Func<List<List<EwfTableCell>>> rowAndColumnSpacerCellCreator ) {
			this.rowSpacerCellCreator = rowSpacerCellCreator;
			this.columnSpacerCellCreator = columnSpacerCellCreator;
			this.rowAndColumnSpacerCellCreator = rowAndColumnSpacerCellCreator;
		}

		internal Func<List<EwfTableCell>> RowSpacerCellCreator { get { return rowSpacerCellCreator; } }
		internal Func<List<EwfTableCell>> ColumnSpacerCellCreator { get { return columnSpacerCellCreator; } }
		internal Func<List<List<EwfTableCell>>> RowAndColumnSpacerCellCreator { get { return rowAndColumnSpacerCellCreator; } }
	}
}