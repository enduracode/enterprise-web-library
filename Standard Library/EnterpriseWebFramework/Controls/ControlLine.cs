using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.CssHandling;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A horizontal line of controls, top-aligned.
	/// </summary>
	[ ParseChildren( ChildrenAsProperties = true, DefaultProperty = "MarkupControls" ) ]
	public class ControlLine: WebControl, ControlTreeDataLoader {
		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public class CssElementCreator: ControlCssElementCreator {
			internal const string CssClass = "ewfControlLine";
			internal const string ItemCssClass = "ewfControlLineItem";

			/// <summary>
			/// Standard Library use only.
			/// </summary>
			public static readonly string[] Selectors = ( "div." + CssClass ).ToSingleElementArray();

			CssElement[] ControlCssElementCreator.CreateCssElements() {
				var itemSelectors = from i in EwfTable.CssElementCreator.CellSelectors select "table > tbody > tr > " + i + "." + ItemCssClass;
				return new[] { new CssElement( "ControlLine", Selectors ), new CssElement( "ControlLineItem", itemSelectors.ToArray() ) };
			}
		}

		private readonly TableRow row;

		// NOTE: Remove this when we have eliminated control lines from markup.
		private readonly List<Control> markupControls = new List<Control>();

		private readonly List<Control> codeControls = new List<Control>();

		/// <summary>
		/// Gets or sets the vertical alignment of this control line.
		/// </summary>
		public TableCellVerticalAlignment VerticalAlignment { get; set; }

		/// <summary>
		/// Gets or sets whether items are separated with a pipe character.
		/// </summary>
		public bool ItemsSeparatedWithPipe { get; set; }

		/// <summary>
		/// Creates a new instance of a Control Line with the given controls.
		/// </summary>
		public ControlLine( params Control[] controls ) {
			var table = TableOps.CreateUnderlyingTable();
			table.Rows.Add( row = new TableRow() );
			base.Controls.Add( table );
			AddControls( controls );
			VerticalAlignment = TableCellVerticalAlignment.NotSpecified;
		}

		/// <summary>
		/// Do not call this, and do not place control lines in markup.
		/// </summary>
		// NOTE: Remove this when we have eliminated control lines from markup.
		public ControlLine(): this( new Control[ 0 ] ) {}

		/// <summary>
		/// Do not use this property; it will be deleted.
		/// </summary>
		// NOTE: Remove this when we have eliminated control lines from markup.
		public List<Control> MarkupControls { get { return markupControls; } }

		/// <summary>
		/// Adds the specified controls to the line.
		/// </summary>
		public void AddControls( params Control[] controls ) {
			codeControls.AddRange( controls );
		}

		void ControlTreeDataLoader.LoadData( DBConnection cn ) {
			CssClass = CssClass.ConcatenateWithSpace( CssElementCreator.CssClass );

			var items = markupControls.Concat( codeControls );
			if( ItemsSeparatedWithPipe )
				items = separateControls( items );
			var cells = from i in items
			            select
				            new TableCell
					            {
						            CssClass =
							            StringTools.ConcatenateWithDelimiter( " ",
							                                                  EwfTable.CssElementCreator.AllCellAlignmentsClass,
							                                                  TableCellVerticalAlignmentOps.Class( VerticalAlignment ),
							                                                  CssElementCreator.ItemCssClass )
					            }.AddControlsReturnThis( i );
			row.Cells.AddRange( cells.ToArray() );
		}

		private IEnumerable<Control> separateControls( IEnumerable<Control> controls ) {
			if( !controls.Any() )
				return controls;

			var interleavedList = new List<Control>();
			foreach( var control in controls.Take( controls.Count() - 1 ) ) {
				interleavedList.Add( control );
				interleavedList.Add( "|".GetLiteralControl() );
			}
			interleavedList.Add( controls.Last() );
			return interleavedList;
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Div; } }
	}
}