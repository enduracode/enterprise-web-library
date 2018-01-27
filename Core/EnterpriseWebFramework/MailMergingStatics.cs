using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.UI;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.MailMerging.RowTree;
using Humanizer;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public static class MailMergingStatics {
		private static readonly ElementClass fieldTreeClass = new ElementClass( "ewfMft" );
		private static readonly ElementClass fieldTreeChildClass = new ElementClass( "ewfMftc" );
		private static readonly ElementClass rowTreeClass = new ElementClass( "ewfMrt" );
		private static readonly ElementClass rowTreeChildClass = new ElementClass( "ewfMrtc" );

		[ UsedImplicitly ]
		private class CssElementCreator: ControlCssElementCreator {
			IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() {
				return new[]
					{
						new CssElement( "MergeFieldTreeContainer", "div.{0}".FormatWith( fieldTreeClass.ClassName ) ),
						new CssElement( "MergeFieldTreeChildContainer", "div.{0}".FormatWith( fieldTreeChildClass.ClassName ) ),
						new CssElement( "MergeRowTreeContainer", "div.{0}".FormatWith( rowTreeClass.ClassName ) ),
						new CssElement( "MergeRowTreeChildContainer", "div.{0}".FormatWith( rowTreeChildClass.ClassName ) )
					};
			}
		}

		/// <summary>
		/// Creates a merge-field-tree display from this empty row tree.
		/// </summary>
		/// <param name="emptyRowTree">The empty merge row tree.</param>
		/// <param name="name">The plural name of the data type at the top level in the row tree, e.g. Clients.</param>
		public static Control ToFieldTreeDisplay( this MergeRowTree emptyRowTree, string name ) {
			return new Block( getFieldTree( name, emptyRowTree.Rows ) ) { CssClass = fieldTreeClass.ClassName };
		}

		private static Control getFieldTree( string name, IEnumerable<MergeRow> emptyRowTree ) {
			var singleRow = emptyRowTree.Single();

			var table = EwfTable.Create( caption: name, headItems: new EwfTableItem( "Field name", "Description" ).ToCollection() );
			table.AddData( singleRow.Values, i => new EwfTableItem( getFieldNameCellText( i ), i.GetDescription() ) );
			table.AddData(
				singleRow.Children,
				i => new EwfTableItem(
					new Block( getFieldTree( i.NodeName, i.Rows ) ) { CssClass = fieldTreeChildClass.ClassName }.ToCell( new TableCellSetup( fieldSpan: 2 ) ) ) );
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
	}
}