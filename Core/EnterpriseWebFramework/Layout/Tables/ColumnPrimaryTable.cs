using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.DataAccess;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A column primary table. Do not use this control in markup.
	/// </summary>
	public class ColumnPrimaryTable: WebControl, ControlTreeDataLoader {
		private readonly bool hideIfEmpty;
		private readonly EwfTableStyle style;
		private readonly ReadOnlyCollection<string> classes;
		private readonly string caption;
		private readonly string subCaption;
		private readonly bool allowExportToExcel;
		private readonly ReadOnlyCollection<Tuple<string, Action>> tableActions;
		private readonly EwfTableField[] specifiedFields;
		private readonly ReadOnlyCollection<EwfTableItem> headItems;
		private readonly int firstDataFieldIndex;
		private readonly List<ColumnPrimaryItemGroup> itemGroups;

		/// <summary>
		/// Creates a table with one item group.
		/// </summary>
		/// <param name="hideIfEmpty">Set to true if you want this table to hide itself if it has no content rows.</param>
		/// <param name="style">The table's style.</param>
		/// <param name="classes">The classes on the table.</param>
		/// <param name="caption">The caption that appears above the table. Do not pass null. Setting this to the empty string means the table will have no caption.
		/// </param>
		/// <param name="subCaption">The sub caption that appears directly under the caption. Do not pass null. Setting this to the empty string means there will be
		/// no sub caption.</param>
		/// <param name="allowExportToExcel">Set to true if you want an Export to Excel action link to appear. This will only work if the table consists of simple
		/// text (no controls).</param>
		/// <param name="tableActions">Table action buttons. This could be used to add a new customer or other entity to the table, for example.</param>
		/// <param name="fields">The table's fields. Do not pass an empty array.</param>
		/// <param name="headItems">The table's head items.</param>
		/// <param name="firstDataFieldIndex">The index of the first data field.</param>
		/// <param name="items">The items.</param>
		public ColumnPrimaryTable( bool hideIfEmpty = false, EwfTableStyle style = EwfTableStyle.Standard, IEnumerable<string> classes = null, string caption = "",
		                           string subCaption = "", bool allowExportToExcel = false, IEnumerable<Tuple<string, Action>> tableActions = null,
		                           IEnumerable<EwfTableField> fields = null, IEnumerable<EwfTableItem> headItems = null, int firstDataFieldIndex = 0,
		                           IEnumerable<EwfTableItem> items = null )
			: this(
				hideIfEmpty,
				style,
				classes,
				caption,
				subCaption,
				allowExportToExcel,
				tableActions,
				fields,
				headItems,
				firstDataFieldIndex,
				items != null ? new List<ColumnPrimaryItemGroup> { new ColumnPrimaryItemGroup( null, items: items ) } : null ) {}

		/// <summary>
		/// Creates a table with multiple item groups.
		/// </summary>
		/// <param name="hideIfEmpty">Set to true if you want this table to hide itself if it has no content rows.</param>
		/// <param name="style">The table's style.</param>
		/// <param name="classes">The classes on the table.</param>
		/// <param name="caption">The caption that appears above the table. Do not pass null. Setting this to the empty string means the table will have no caption.
		/// </param>
		/// <param name="subCaption">The sub caption that appears directly under the caption. Do not pass null. Setting this to the empty string means there will be
		/// no sub caption.</param>
		/// <param name="allowExportToExcel">Set to true if you want an Export to Excel action link to appear. This will only work if the table consists of simple
		/// text (no controls).</param>
		/// <param name="tableActions">Table action buttons. This could be used to add a new customer or other entity to the table, for example.</param>
		/// <param name="fields">The table's fields. Do not pass an empty array.</param>
		/// <param name="headItems">The table's head items.</param>
		/// <param name="firstDataFieldIndex">The index of the first data field.</param>
		/// <param name="itemGroups">The item groups.</param>
		// NOTE: Change the Tuple for tableActions to a named type.
		public ColumnPrimaryTable( bool hideIfEmpty = false, EwfTableStyle style = EwfTableStyle.Standard, IEnumerable<string> classes = null, string caption = "",
		                           string subCaption = "", bool allowExportToExcel = false, IEnumerable<Tuple<string, Action>> tableActions = null,
		                           IEnumerable<EwfTableField> fields = null, IEnumerable<EwfTableItem> headItems = null, int firstDataFieldIndex = 0,
		                           IEnumerable<ColumnPrimaryItemGroup> itemGroups = null ) {
			this.hideIfEmpty = hideIfEmpty;
			this.style = style;
			this.classes = ( classes ?? new string[ 0 ] ).ToList().AsReadOnly();
			this.caption = caption;
			this.subCaption = subCaption;
			this.allowExportToExcel = allowExportToExcel;
			this.tableActions = ( tableActions ?? new Tuple<string, Action>[ 0 ] ).ToList().AsReadOnly();

			if( fields != null ) {
				if( !fields.Any() )
					throw new ApplicationException( "If fields are specified, there must be at least one of them." );
				specifiedFields = fields.ToArray();
			}

			this.headItems = ( headItems ?? new EwfTableItem[ 0 ] ).ToList().AsReadOnly();
			this.firstDataFieldIndex = firstDataFieldIndex;
			this.itemGroups = ( itemGroups ?? new ColumnPrimaryItemGroup[ 0 ] ).ToList();
		}

		void ControlTreeDataLoader.LoadData() {
			if( hideIfEmpty && itemGroups.All( itemGroup => !itemGroup.Items.Any() ) ) {
				Visible = false;
				return;
			}

			EwfTable.SetUpTableAndCaption( this, style, classes, caption, subCaption );

			var itemSetupLists = new[] { headItems }.Concat( itemGroups.Select( i => i.Items ) ).Select( i => i.Select( j => j.Setup.FieldOrItemSetup ) );
			var allItemSetups = itemSetupLists.SelectMany( i => i ).ToList();
			var columnWidthFactor = EwfTable.GetColumnWidthFactor( allItemSetups );
			foreach( var itemSetups in itemSetupLists.Where( i => i.Any() ) )
				Controls.Add( new WebControl( HtmlTextWriterTag.Colgroup ).AddControlsReturnThis( itemSetups.Select( i => EwfTable.GetColControl( i, columnWidthFactor ) ) ) );

			var fields = EwfTable.GetFields( specifiedFields, headItems, itemGroups.SelectMany( i => i.Items ) );
			var cellPlaceholderListsForItems = TableOps.BuildCellPlaceholderListsForItems( headItems.Concat( itemGroups.SelectMany( i => i.Items ) ).ToList(),
			                                                                               fields.Length );

			// Pivot the cell placeholders from column primary into row primary format.
			var cellPlaceholderListsForRows =
				Enumerable.Range( 0, fields.Length ).Select(
					field => Enumerable.Range( 0, allItemSetups.Count ).Select( item => cellPlaceholderListsForItems[ item ][ field ] ).ToList() ).ToList();

			var headRows = TableOps.BuildRows( cellPlaceholderListsForRows.Take( firstDataFieldIndex ).ToList(),
			                                   fields.Select( i => i.FieldOrItemSetup ).ToList().AsReadOnly(),
			                                   null,
			                                   allItemSetups.AsReadOnly(),
			                                   allItemSetups.Count,
			                                   true );
			var bodyRows = TableOps.BuildRows( cellPlaceholderListsForRows.Skip( firstDataFieldIndex ).ToList(),
			                                   fields.Select( i => i.FieldOrItemSetup ).ToList().AsReadOnly(),
			                                   false,
			                                   allItemSetups.AsReadOnly(),
			                                   headItems.Count,
			                                   true );

			// We can't easily put the head fields in thead because we don't have a way of verifying that cells don't cross between head and data fields.
			Controls.Add( new WebControl( HtmlTextWriterTag.Tbody ).AddControlsReturnThis( headRows.Concat( bodyRows ) ) );

			EwfTable.AssertAtLeastOneCellPerField( fields, cellPlaceholderListsForItems );
		}

		/// <summary>
		/// Returns the table element, which represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Table; } }
	}
}