using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	[ Obsolete( "Guaranteed through 30 November 2020." ) ]
	[ ParseChildren( ChildrenAsProperties = true, DefaultProperty = "MarkupControls" ) ]
	public class ControlLine: WebControl, ControlTreeDataLoader {
		/// <summary>
		/// EWL use only.
		/// </summary>
		public class CssElementCreator: ControlCssElementCreator {
			internal const string CssClass = "ewfControlLine";
			internal const string ItemCssClass = "ewfControlLineItem";

			/// <summary>
			/// EWL use only.
			/// </summary>
			public static readonly IReadOnlyCollection<string> Selectors = ( "div." + CssClass ).ToCollection();

			IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() {
				var itemSelectors = from i in TableCssElementCreator.CellSelectors select "table > tbody > tr > " + i + "." + ItemCssClass;
				return new[] { new CssElement( "ControlLine", Selectors.ToArray() ), new CssElement( "ControlLineItem", itemSelectors.ToArray() ) };
			}
		}

		private readonly TableRow row;
		private readonly List<Control> items = new List<Control>();

		/// <summary>
		/// Gets or sets the vertical alignment of this control line.
		/// </summary>
		public TableCellVerticalAlignment VerticalAlignment { get; set; }

		/// <summary>
		/// Gets or sets whether items are separated with a pipe character.
		/// </summary>
		public bool ItemsSeparatedWithPipe { get; set; }

		[ Obsolete( "Guaranteed through 30 November 2020." ) ]
		public ControlLine( params Control[] controls ) {
			var table = TableStatics.CreateUnderlyingTable();
			table.Rows.Add( row = new TableRow() );
			base.Controls.Add( table );
			AddControls( controls );
			VerticalAlignment = TableCellVerticalAlignment.NotSpecified;
		}

		[ Obsolete( "Guaranteed through 30 November 2020." ) ]
		public void AddControls( params Control[] controls ) {
			items.AddRange( controls );
		}

		void ControlTreeDataLoader.LoadData() {
			CssClass = CssClass.ConcatenateWithSpace( CssElementCreator.CssClass );

			var cells = from i in ItemsSeparatedWithPipe ? separateControls( items ) : items
			            select new TableCell
				            {
					            CssClass = StringTools.ConcatenateWithDelimiter(
						            " ",
						            TableCssElementCreator.AllCellAlignmentsClass.ClassName,
						            tableCellVerticalAlignmentClass( VerticalAlignment ),
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
				interleavedList.AddRange( "|".ToComponents().GetControls() );
			}
			interleavedList.Add( controls.Last() );
			return interleavedList;
		}

		private string tableCellVerticalAlignmentClass( TableCellVerticalAlignment verticalAlignment ) {
			switch( verticalAlignment ) {
				case TableCellVerticalAlignment.Top:
					return "ewfTcTop";
				case TableCellVerticalAlignment.Middle:
					return "ewfTcMiddle";
				case TableCellVerticalAlignment.Bottom:
					return "ewfTcBottom";
				case TableCellVerticalAlignment.BaseLine:
					return "ewfTcBaseLine";
				default:
					return "";
			}
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey => HtmlTextWriterTag.Div;
	}
}