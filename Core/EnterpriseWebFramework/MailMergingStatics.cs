using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.UI;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.MailMerging;
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

		/// <summary>
		/// Creates a display from this row tree. The display is an ordered list of rows, in which each row is a form-item list of values and a section for each
		/// child row-tree display.
		/// </summary>
		/// <param name="rowTree">The merge row tree.</param>
		/// <param name="fieldNameTree">The fields that you want to include in the display. Pass null for all.</param>
		/// <param name="omitListIfSingleRow">Pass true to omit the root ordered-list component if the tree has exactly one row.</param>
		/// <param name="useSubtractiveMode">Pass true if you want the field-name tree to represent excluded fields, rather than included fields.</param>
		public static FlowComponent ToRowTreeDisplay(
			this MergeRowTree rowTree, MergeFieldNameTree fieldNameTree, bool omitListIfSingleRow = false, bool useSubtractiveMode = false ) =>
			new GenericFlowContainer(
				omitListIfSingleRow && rowTree.Rows.Count() == 1
					? getRow( rowTree.Rows.Single(), fieldNameTree, useSubtractiveMode )
					: new StackList( from i in rowTree.Rows select getRow( i, fieldNameTree, useSubtractiveMode ).ToComponentListItem() ).ToCollection(),
				classes: rowTreeClass );

		private static IReadOnlyCollection<FlowComponent> getRow( MergeRow row, MergeFieldNameTree fieldNameTree, bool useSubtractiveMode ) {
			var values = FormItemList.CreateGrid(
				2,
				generalSetup: new FormItemListSetup( hideIfEmpty: true ),
				items: ( useSubtractiveMode
					         ? row.Values.Where( mergeValue => fieldNameTree?.FieldNames.All( i => i != mergeValue.Name ) ?? false )
					         : fieldNameTree?.FieldNames.Select( fieldName => row.Values.Single( i => i.Name == fieldName ) ) ?? row.Values ).Select(
					mergeValue => {
						IReadOnlyCollection<PhrasingComponent> value = null;
						if( mergeValue is MergeValue<string> stringValue )
							value = stringValue.Evaluate( false ).ToComponents();

						// Use ApplicationException instead of MailMergingException because the field names can easily be validated before this method is called.
						return value == null
							       ? throw new ApplicationException( "Merge field {0} evaluates to an unsupported type.".FormatWith( mergeValue.Name ) )
							       : value.ToFormItem( label: mergeValue.Name.ToComponents() );
					} )
				.Materialize() );

			var children = ( useSubtractiveMode
				                 ? row.Children.Select(
						                 childRowTree => {
							                 MergeFieldNameTree childFieldNameTree = null;
							                 if( fieldNameTree != null ) {
								                 var childNameAndFieldNameTree = fieldNameTree.ChildNamesAndChildren.SingleOrDefault( i => i.Item1 == childRowTree.NodeName );
								                 childFieldNameTree = childNameAndFieldNameTree != null
									                                      ? childNameAndFieldNameTree.Item2
									                                      : new MergeFieldNameTree( Enumerable.Empty<string>() );
							                 }
							                 return new { rowTree = childRowTree, fieldNameTree = childFieldNameTree };
						                 } )
					                 .Where( i => i.fieldNameTree != null )
				                 : fieldNameTree?.ChildNamesAndChildren.Select(
					                   childNameAndFieldNameTree => new
						                   {
							                   rowTree = row.Children.Single( i => i.NodeName == childNameAndFieldNameTree.Item1 ),
							                   fieldNameTree = childNameAndFieldNameTree.Item2
						                   } ) ?? row.Children.Select( childRowTree => new { rowTree = childRowTree, fieldNameTree = (MergeFieldNameTree)null } ) )
				.Where( child => child.rowTree.Rows.Any() )
				.Select(
					child => new Section(
						child.rowTree.NodeName,
						new StackList( child.rowTree.Rows.Select( i => getRow( i, child.fieldNameTree, useSubtractiveMode ).ToComponentListItem() ) ).ToCollection() ) )
				.Materialize();

			return children.Any()
				       ? values.ToCollection<FlowComponent>().Append( new GenericFlowContainer( children, classes: rowTreeChildClass ) ).Materialize()
				       : values.ToCollection();
		}
	}
}