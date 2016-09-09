using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.MailMerging.RowTree;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A merge field tree display.
	/// </summary>
	public sealed class MergeFieldTree: WebControl {
		/// <summary>
		/// Creates a merge field tree.
		/// </summary>
		/// <param name="name">The plural name of the data type at the top level in the row tree, e.g. Clients.</param>
		/// <param name="emptyRowTree">The merge row tree that will be used to draw the field tree.</param>
		public MergeFieldTree( string name, MergeRowTree emptyRowTree ) {
			CssClass = CssClass.ConcatenateWithSpace( "ewfMergeFieldTree" );
			Controls.Add( buildTree( name, emptyRowTree.Rows ) );
		}

		private DynamicTable buildTree( string name, IEnumerable<MergeRow> emptyRowTree ) {
			var singleRow = emptyRowTree.Single();

			var table = new DynamicTable( new EwfTableColumn( "Field name" ), new EwfTableColumn( "Description" ) ) { Caption = name };
			foreach( var field in singleRow.Values )
				table.AddTextRow( getFieldNameCellText( field ), field.GetDescription() );

			foreach( var child in singleRow.Children ) {
				var panel = new Panel();
				panel.Style.Add( HtmlTextWriterStyle.MarginLeft, "2em" );
				panel.Controls.Add( buildTree( child.NodeName, child.Rows ) );

				table.AddRow( panel.ToCell( new TableCellSetup( fieldSpan: 2 ) ) );
			}

			return table;
		}

		private string getFieldNameCellText( MergeValue field ) {
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