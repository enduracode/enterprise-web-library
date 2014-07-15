using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// An item in a table.
	/// </summary>
	public class EwfTableItem {
		internal readonly EwfTableItemSetup Setup;
		internal readonly ReadOnlyCollection<EwfTableCell> Cells;

		/// <summary>
		/// Creates a table item.
		/// </summary>
		/// <param name="cells">The cells in this item.</param>
		public EwfTableItem( params EwfTableCell[] cells ): this( cells as IEnumerable<EwfTableCell> ) {}

		/// <summary>
		/// Creates a table item.
		/// </summary>
		/// <param name="cells">The cells in this item.</param>
		public EwfTableItem( IEnumerable<EwfTableCell> cells ): this( null, cells ) {}

		/// <summary>
		/// Creates a table item.
		/// </summary>
		/// <param name="setup">The setup object for the item.</param>
		/// <param name="cells">The cells in this item.</param>
		public EwfTableItem( EwfTableItemSetup setup, params EwfTableCell[] cells ): this( setup, cells as IEnumerable<EwfTableCell> ) {}

		/// <summary>
		/// Creates a table item.
		/// </summary>
		/// <param name="setup">The setup object for the item.</param>
		/// <param name="cells">The cells in this item.</param>
		public EwfTableItem( EwfTableItemSetup setup, IEnumerable<EwfTableCell> cells ) {
			Setup = setup ?? new EwfTableItemSetup();

			var cellList = cells.ToList();
			if( !cellList.Any() )
				throw new ApplicationException( "Cell array must have at least one item." );
			Cells = cellList.AsReadOnly();
		}
	}
}