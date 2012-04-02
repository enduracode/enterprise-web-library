using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.MailMerging.RowTree;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A merge field tree display.
	/// </summary>
	public class MergeFieldTree: WebControl, ControlTreeDataLoader {
		private string name;
		private IEnumerable<MergeRow> emptyRowTree;

		/// <summary>
		/// Creates a merge field tree.
		/// </summary>
		public MergeFieldTree() {}

		/// <summary>
		/// Creates a merge field tree and sets the name and empty row tree.
		/// </summary>
		public MergeFieldTree( string name, IEnumerable<MergeRow> emptyRowTree ) {
			SetNameAndEmptyRowTree( name, emptyRowTree );
		}

		/// <summary>
		/// Sets the name and the merge row tree that will be used to draw the field tree. The name should be the plural name of the data type at the top level in
		/// the row tree, e.g. Clients.
		/// </summary>
		public void SetNameAndEmptyRowTree( string name, IEnumerable<MergeRow> emptyRowTree ) {
			this.name = name;
			this.emptyRowTree = emptyRowTree;
		}

		void ControlTreeDataLoader.LoadData( DBConnection cn ) {
			CssClass = CssClass.ConcatenateWithSpace( "ewfMergeFieldTree" );
			Controls.Add( buildTree( cn, name, emptyRowTree ) );
		}

		private static DynamicTable buildTree( DBConnection cn, string name, IEnumerable<MergeRow> emptyRowTree ) {
			var singleRow = emptyRowTree.Single();

			var table = new DynamicTable( new EwfTableColumn( "Field name" ), new EwfTableColumn( "Description" ) ) { Caption = name };
			foreach( var field in singleRow.Values )
				table.AddTextRow( getFieldNameCellText( field ), field.GetDescription( cn ) );

			foreach( var child in singleRow.Children ) {
				var panel = new Panel();
				panel.Style.Add( HtmlTextWriterStyle.MarginLeft, "2em" );
				panel.Controls.Add( buildTree( cn, child.Name, child.Rows ) );

				table.AddRow( new EwfTableCell( panel ) { FieldSpan = 2 } );
			}

			return table;
		}

		private static string getFieldNameCellText( MergeValue field ) {
			var name = field.Name;
			var msWordName = field.MsWordName;
			if( name == msWordName )
				return name;
			using( var writer = new StringWriter() ) {
				writer.WriteLine( name );
				writer.WriteLine( "MS Word field name: " + msWordName );
				return writer.ToString();
			}
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Div; } }
	}
}