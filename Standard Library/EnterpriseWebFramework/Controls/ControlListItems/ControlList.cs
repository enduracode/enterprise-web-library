using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A list of controls.
	/// </summary>
	public class ControlList: WebControl, ControlTreeDataLoader {
		private readonly List<EwfTableCell> codeControls = new List<EwfTableCell>();

		/// <summary>
		/// Gets or sets the number of columns this control list will use when rendering its items.
		/// </summary>
		public int NumberOfColumns { get; set; }

		/// <summary>
		/// Gets or sets the method used to create empty cells if they are needed to complete the last row in the table.
		/// </summary>
		public Func<EwfTableCell> EmptyCellCreator { get; set; }

		/// <summary>
		/// Gets or sets whether this control list will have standard styling.
		/// </summary>
		public bool IsStandard { get; set; }

		/// <summary>
		/// Set to true if you want this ControlList to hide itself if it has no controls.
		/// </summary>
		public bool HideIfEmpty { get; set; }

		/// <summary>
		/// Set the caption control to display first in this ControlList.
		/// </summary>
		public EwfTableCell CaptionControl { get; set; }

		// NOTE: Support custom row and column setups for both data rows/columns and spacer rows/columns.

		/// <summary>
		/// Gets or sets the spacer cell setup. Do not set this to null.
		/// </summary>
		public ControlListSpacerCellSetup SpacerCellSetup { get; set; }

		/// <summary>
		/// Creates an empty control list.
		/// </summary>
		public static ControlList Create( bool isStandard ) {
			return new ControlList { IsStandard = isStandard };
		}

		/// <summary>
		/// Creates a control list out of the given list of strings.
		/// </summary>
		public static ControlList CreateWithText( bool isStandard, params string[] text ) {
			var cl = Create( isStandard );
			cl.AddText( text );
			return cl;
		}

		/// <summary>
		/// Creates a control list and adds the specified controls to it.
		/// </summary>
		public static ControlList CreateWithControls( bool isStandard, params EwfTableCell[] controls ) {
			var cl = Create( isStandard );
			cl.AddControls( controls );
			return cl;
		}

		/// <summary>
		/// Markup use only.
		/// </summary>
		public ControlList() {
			NumberOfColumns = 1;
			EmptyCellCreator = () => new EwfTableCell( "" );
			IsStandard = true;
			SpacerCellSetup = new ControlListSpacerCellSetup();
		}

		/// <summary>
		/// Add the given list of strings to the control list. Do not pass null for any of the strings. If you do, it will be converted to the empty string.
		/// </summary>
		public void AddText( params string[] text ) {
			AddControls( text.Select( s => new EwfTableCell( s ) ).ToArray() );
		}

		/// <summary>
		/// Adds the specified controls to the list.
		/// </summary>
		public void AddControls( params EwfTableCell[] controls ) {
			codeControls.AddRange( controls );
		}

		void ControlTreeDataLoader.LoadData( DBConnection cn ) {
			var table = new DynamicTable { IsStandard = false };

			var controls = codeControls;
			if( HideIfEmpty && controls.Count == 0 ) {
				Visible = false;
				return;
			}
			var firstPassThroughRowLoop = true;
			for( var i = 0; i < controls.Count; i += NumberOfColumns ) {
				// spacer row
				if( !firstPassThroughRowLoop ) {
					var spacerRowCount = SpacerCellSetup.RowSpacerCellCreator().Count;
					for( var spacerRowIndex = 0; spacerRowIndex < spacerRowCount; spacerRowIndex += 1 ) {
						var firstPassThroughSpacerRowColumnLoop = true;
						var spacerRowCells = new List<EwfTableCell>();
						for( var spacerRowColumnIndex = 0; spacerRowColumnIndex < NumberOfColumns; spacerRowColumnIndex += 1 ) {
							if( !firstPassThroughSpacerRowColumnLoop )
								spacerRowCells.AddRange( SpacerCellSetup.RowAndColumnSpacerCellCreator()[ spacerRowIndex ] );
							spacerRowCells.Add( SpacerCellSetup.RowSpacerCellCreator()[ spacerRowIndex ] );
							firstPassThroughSpacerRowColumnLoop = false;
						}
						table.AddRow( spacerRowCells.ToArray() );
					}
				}

				// content row
				var firstPassThroughColumnLoop = true;
				var cells = new List<EwfTableCell>();
				for( var columnIndex = 0; columnIndex < NumberOfColumns; columnIndex += 1 ) {
					// spacer cells
					if( !firstPassThroughColumnLoop )
						cells.AddRange( SpacerCellSetup.ColumnSpacerCellCreator() );

					// content cell

					if( firstPassThroughRowLoop && CaptionControl != null ) {
						cells.Add( CaptionControl );
						i -= NumberOfColumns;
					}
					else {
						var controlIndex = i + columnIndex;
						if( controlIndex < controls.Count )
							cells.Add( controls[ controlIndex ] );
						else
							cells.Add( EmptyCellCreator() );
					}
					firstPassThroughColumnLoop = false;
				}
				table.AddRow( cells.ToArray() );

				firstPassThroughRowLoop = false;
			}

			Controls.Add( table );
		}

		/// <summary>
		/// Returns the div tag, which represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Div; } }

		/// <summary>
		/// Renders this control after applying the appropriate CSS classes.
		/// </summary>
		protected override void Render( HtmlTextWriter writer ) {
			CssClass = CssClass.ConcatenateWithSpace( "ewfControlList" );
			if( IsStandard )
				CssClass = CssClass.ConcatenateWithSpace( "ewfStandard" );
			base.Render( writer );
		}
	}
}