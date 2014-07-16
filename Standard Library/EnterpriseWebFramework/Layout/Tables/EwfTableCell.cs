using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A cell in a table.
	/// </summary>
	public class EwfTableCell: CellPlaceholder {
		public static implicit operator EwfTableCell( string text ) {
			return new EwfTableCell( new TableCellSetup(), text );
		}

		public static implicit operator EwfTableCell( Control control ) {
			return new EwfTableCell( new TableCellSetup(), control );
		}

		internal readonly TableCellSetup Setup;
		internal readonly IEnumerable<Control> Controls;
		private readonly string simpleText;

		private string cssClass = "";
		private int fieldSpan = 1;
		private int itemSpan = 1;

		[ Obsolete( "Guaranteed through 30 September 2014. Pass at cell creation time with ToCell method." ) ]
		public ClickScript ClickScript { get; set; }

		string CellPlaceholder.SimpleText { get { return simpleText; } }

		[ Obsolete( "Guaranteed through 30 September 2014. Pass at cell creation time with ToCell method." ) ]
		public string CssClass { get { return cssClass; } set { cssClass = value; } }

		[ Obsolete( "Guaranteed through 30 September 2014. Pass at cell creation time with ToCell method." ) ]
		public TextAlignment TextAlignment { get; set; }

		[ Obsolete( "Guaranteed through 30 September 2014. Pass at cell creation time with ToCell method." ) ]
		public string ToolTip { get; set; }

		[ Obsolete( "Guaranteed through 30 September 2014. Pass at cell creation time with ToCell method." ) ]
		public Control ToolTipControl { get; set; }

		[ Obsolete( "Guaranteed through 30 September 2014. Pass at cell creation time with ToCell method." ) ]
		public int FieldSpan { get { return fieldSpan; } set { fieldSpan = value; } }

		[ Obsolete( "Guaranteed through 30 September 2014. Pass at cell creation time with ToCell method." ) ]
		public int ItemSpan { get { return itemSpan; } set { itemSpan = value; } }

		internal EwfTableCell( TableCellSetup setup, string text ) {
			Setup = setup;
			Controls = ( text ?? "" ).GetLiteralControl().ToSingleElementArray();
			simpleText = text;
			setObsoleteProperties();
		}

		internal EwfTableCell( TableCellSetup setup, Control control ): this( setup, control != null ? control.ToSingleElementArray() : new Control[ 0 ] ) {}

		internal EwfTableCell( TableCellSetup setup, IEnumerable<Control> controls ) {
			Setup = setup;
			Controls = controls.Any() ? controls : "".GetLiteralControl().ToSingleElementArray();
			simpleText = null;
			setObsoleteProperties();
		}

		private void setObsoleteProperties() {
			fieldSpan = Setup.FieldSpan;
			itemSpan = Setup.ItemSpan;
			cssClass = StringTools.ConcatenateWithDelimiter( " ", Setup.Classes.ToArray() );
			TextAlignment = Setup.TextAlignment;
			ClickScript = Setup.ClickScript;
			ToolTip = Setup.ToolTip;
			ToolTipControl = Setup.ToolTipControl;
		}

		[ Obsolete( "Guaranteed through 30 September 2014. Use implicit conversion or string.ToCell instead." ) ]
		public EwfTableCell( string text ): this( new TableCellSetup(), text ) {}

		[ Obsolete( "Guaranteed through 30 September 2014. Use implicit conversion or Control.ToCell instead." ) ]
		public EwfTableCell( Control control ): this( new TableCellSetup(), control ) {}
	}
}