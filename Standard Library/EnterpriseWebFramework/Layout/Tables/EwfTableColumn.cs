using System.Web.UI.WebControls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// Defines a column in an EWF table. Every cell in this column will share the given width.
	/// </summary>
	public class EwfTableColumn {
		internal ColumnSetup ColumnSetup { get; private set; }
		internal EwfTableCell HeaderCell { get; private set; }

		/// <summary>
		/// Creates an EWF table column with the specified header cell, width, and CSS class.
		/// </summary>
		public EwfTableColumn( EwfTableCell headerCell, Unit width, string cssClass ) {
			init( headerCell, width, cssClass );
		}

		/// <summary>
		/// Creates an EWF table column with the specified header cell text.
		/// </summary>
		public EwfTableColumn( string headerCellText ) {
			init( new EwfTableCell( headerCellText ), Unit.Empty, "" );
		}

		/// <summary>
		/// Creates an EWF table column with the specified header cell text and the specified width.
		/// </summary>
		public EwfTableColumn( string headerCellText, Unit width ) {
			init( new EwfTableCell( headerCellText ), width, "" );
		}

		/// <summary>
		/// Creates an EWF table column with the specified header cell text and the specified CSS class.
		/// </summary>
		public EwfTableColumn( string headerCellText, string cssClass ) {
			init( new EwfTableCell( headerCellText ), Unit.Empty, cssClass );
		}

		/// <summary>
		/// Creates an EWF table column with the specified header cell.
		/// </summary>
		public EwfTableColumn( EwfTableCell headerCell ) {
			init( headerCell, Unit.Empty, "" );
		}

		/// <summary>
		/// Creates an EWF table column with the specified header cell and width.
		/// </summary>
		public EwfTableColumn( EwfTableCell headerCell, Unit width ) {
			init( headerCell, width, "" );
		}

		/// <summary>
		/// Creates an EWF table column with the specified header cell and CSS class.
		/// </summary>
		public EwfTableColumn( EwfTableCell headerCell, string cssClass ) {
			init( headerCell, Unit.Empty, cssClass );
		}

		/// <summary>
		/// Creates an EWF table column with the specified header cell text and the specified width and CSS class.
		/// </summary>
		public EwfTableColumn( string headerCellText, Unit width, string cssClass ) {
			init( new EwfTableCell( headerCellText ), width, cssClass );
		}

		private void init( EwfTableCell headerCell, Unit width, string cssClass ) {
			ColumnSetup = new ColumnSetup { Width = width, CssClassOnAllCells = cssClass ?? "" };
			HeaderCell = headerCell;
		}
	}
}