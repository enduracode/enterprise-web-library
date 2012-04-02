using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// Do not use. Use EwfCheckBoxList instead.
	/// </summary>
	public class Checklist: WebControl, INamingContainer, ControlTreeDataLoader {
		private const string checkboxIdPrefix = "cb";
		private readonly DynamicTable table = new DynamicTable();
		private readonly IDictionary<string, BlockCheckBox> idsToCheckBoxes = new Dictionary<string, BlockCheckBox>();
		private readonly List<BlockCheckBox> checkBoxes = new List<BlockCheckBox>();
		private readonly List<BlockCheckBox> itemsWithBindingDefaultValues = new List<BlockCheckBox>();

		/// <summary>
		/// Specifies the number of columns the checklist should use.
		/// </summary>
		public byte NumberOfColumns { private get; set; }

		/// <summary>
		/// A method that changes the association of the item with the given ID.
		/// </summary>
		public delegate void AssociationChanger( DBConnection cn, string itemIdAsString );

		/// <summary>
		/// Sets the method that will associate an item.
		/// </summary>
		public AssociationChanger Associator { private get; set; }

		/// <summary>
		/// Sets the method that will dissociate an item.
		/// </summary>
		public AssociationChanger Dissociator { private get; set; }

		/// <summary>
		/// The caption to show above the checklist. Do not pass null.
		/// </summary>
		public string Caption { private get; set; }

		/// <summary>
		/// True if standard EWF styles should be applied to this control.
		/// </summary>
		public bool IsStandard { set { table.IsStandard = value; } }

		/// <summary>
		/// EWF ToolTip to display on this control. Setting ToolTipControl will ignore this property.
		/// </summary>
		public override string ToolTip { get; set; }

		/// <summary>
		/// Control to display inside the tool tip. Do not pass null. This will ignore the ToolTip property.
		/// </summary>
		public Control ToolTipControl { get; set; }

		/// <summary>
		/// Adds a checkbox to the beginning of this CheckList that can be used by the user to check or uncheck all of the options at once.
		/// </summary>
		public bool IncludeSelectDeselectAllBox { get; set; }

		/// <summary>
		/// Returns the number of checked boxes in the list.
		/// </summary>
		public int NumberOfAssociatedItems { get { return checkBoxes.Count( cb => cb.Checked ); } }

		/// <summary>
		/// Do not call this, and do not place checklists in markup.
		/// </summary>
		// NOTE: Remove this when we have eliminated checklists from markup.
		public Checklist() {
			PreRender += preRender;
			NumberOfColumns = 1;
			Caption = "";
		}

		/// <summary>
		/// Create a new CheckList with given method to associate and dissociate items. Pass null for associator and dissociator if you do not wish to use them.
		/// </summary>
		public Checklist( AssociationChanger associator, AssociationChanger dissociator ): this() {
			Associator = associator;
			Dissociator = dissociator;
		}

		/// <summary>
		/// Add a new check box to the list given a string representation of its ID and its label.
		/// This item will be unassociated. If the current underlying system data indicates this association already exists,
		/// call MarkItemAsAssociated after this.
		/// </summary>
		public void AddItem( string itemIdAsString, string label ) {
			var cb = new BlockCheckBox { Text = label, ID = ( checkboxIdPrefix + itemIdAsString ) };
			checkBoxes.Add( cb );
			idsToCheckBoxes.Add( itemIdAsString, cb );
		}

		/// <summary>
		/// Marks the item with the given ID as associated (checked in the list).
		/// Use this method if the underlying data reflects that this item is associated.
		/// If you'd instead like to default the value of this item to checked, see SetItemDefaultToTrue.
		/// A call to this will wipe out any calls to SetItemDefault applied to this item up to this point.
		/// Precondition: itemIdAsString has already been added using AddItem.
		/// </summary>
		public void MarkItemAsAssociated( string itemIdAsString ) {
			var checkBox = idsToCheckBoxes[ itemIdAsString ];
			itemsWithBindingDefaultValues.Remove( checkBox );
			checkBox.Checked = true;
		}

		/// <summary>
		/// Sets the default value of the checkbox with the given ID to the given value.  The item with the given ID must have
		/// already been added using AddItem.
		/// Calling this with True is very different from calling MarkItemAsAssociated, in that this merely saves the user from having to
		/// check the box themselves, whereas marking the item as associated is a reflection of what the underlying data already is.
		/// The difference is equivalent to loading a date text box with a date from the database vs prefilling the date text box
		/// with today's date for convenience.
		/// Precondition: itemIdAsString has already been added using AddItem.
		/// </summary>
		public void SetItemDefault( string itemIdAsString, bool value ) {
			var checkBox = idsToCheckBoxes[ itemIdAsString ];
			if( value != checkBox.Checked ) {
				if( itemsWithBindingDefaultValues.Contains( checkBox ) )
					itemsWithBindingDefaultValues.Remove( checkBox );
				else
					itemsWithBindingDefaultValues.Add( checkBox );
				checkBox.Checked = value;
			}
		}

		void ControlTreeDataLoader.LoadData( DBConnection cn ) {
			if( IncludeSelectDeselectAllBox ) {
				table.AddActionLink( new ActionButtonSetup( "Select All", new CustomButton( string.Format( @"toggleCheckBoxes('{0}', true)", ClientID ) ) ) );
				table.AddActionLink( new ActionButtonSetup( "Deselect All", new CustomButton( string.Format( @"toggleCheckBoxes('{0}', false)", ClientID ) ) ) );
			}

			table.Caption = Caption;
			var itemsPerColumn = (int)Math.Ceiling( (double)checkBoxes.Count / NumberOfColumns );
			var columnCells = new List<EwfTableCell>();

			for( byte i = 0; i < NumberOfColumns; i++ ) {
				var minIndex = i * itemsPerColumn;
				var maxIndexNotInclusive = Math.Min( ( i + 1 ) * itemsPerColumn, checkBoxes.Count );

				var columnDiv = new HtmlGenericControl( "div" );
				for( var j = minIndex; j < maxIndexNotInclusive; j++ )
					columnDiv.Controls.Add( checkBoxes[ j ] );

				columnCells.Add( new EwfTableCell( columnDiv ) );
			}

			table.AddRow( columnCells.ToArray() );
			Controls.Add( table );

			if( ToolTip != null || ToolTipControl != null )
				new ToolTip( ToolTipControl ?? EnterpriseWebFramework.Controls.ToolTip.GetToolTipTextControl( ToolTip ), this );
		}

		/// <summary>
		/// Returns a list of values for all check boxes that are currently checked.
		/// </summary>
		public List<string> SelectedValues { get { return checkBoxes.Where( cb => cb.Checked ).Select( getItemIdFromCheckBox ).ToList(); } }

		/// <summary>
		/// For every item whose association has changed, fire associate/dissociate method.
		/// </summary>
		public void ModifyData( DBConnection cn ) {
			foreach( var checkBox in
				checkBoxes.Where(
					cb => cb.ValueChangedOnPostBack( AppRequestState.Instance.EwfPageRequestState.PostBackValues ) ^ itemsWithBindingDefaultValues.Contains( cb ) ) ) {
				var itemIdAsString = getItemIdFromCheckBox( checkBox );
				if( checkBox.Checked && Associator != null )
					Associator( cn, itemIdAsString );
				else if( !checkBox.Checked && Dissociator != null )
					Dissociator( cn, itemIdAsString );
			}
		}

		private static string getItemIdFromCheckBox( BlockCheckBox checkBox ) {
			return checkBox.ID.Substring( checkboxIdPrefix.Length );
		}

		/// <summary>
		/// Returns the div tag, which represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Div; } }

		private void preRender( object sender, EventArgs e ) {
			foreach( var checkBox in checkBoxes ) {
				checkBox.AddOnClickJsMethod( "changeCheckBoxColor( this )" );
				if( checkBox.Checked )
					checkBox.CssClass = "checkedChecklistCheckboxDiv";
			}
		}

		/// <summary>
		/// Renders this control after applying the appropriate CSS classes.
		/// </summary>
		protected override void Render( HtmlTextWriter writer ) {
			CssClass = CssClass.ConcatenateWithSpace( "ewfStandardCheckBoxList" );
			base.Render( writer );
		}

		/// <summary>
		/// Returns true if the selections changed on this post back.
		/// </summary>
		public bool SelectionsChangedOnPostBack( PostBackValueDictionary postBackValues ) {
			return checkBoxes.Any( i => i.ValueChangedOnPostBack( postBackValues ) );
		}
	}
}