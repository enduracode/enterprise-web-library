using System.Web.UI;
using System.Web.UI.WebControls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	// NOTE: Convert all setters to optional constructor parameters to make this class immutable.
	/// <summary>
	/// A cell in a table.
	/// </summary>
	public class EwfTableCell: CellPlaceholder {
		private string simpleText;
		private Control control;
		private string cssClass = "";
		private int fieldSpan = 1;
		private int itemSpan = 1;

		/// <summary>
		/// Specifies the click script for this cell.
		/// </summary>
		public ClickScript ClickScript { get; set; }

		string CellPlaceholder.SimpleText { get { return simpleText; } }

		/// <summary>
		/// Sets the control to be in the cell.  If null is passed, the cell will contain a non-breaking space.
		/// </summary>
		internal Control Control {
			get { return control; }
			private set {
				control = value ?? getLiteral( "" );
				simpleText = null;
			}
		}

		/// <summary>
		/// Sets the text to be in the cell (HTML encoded).  If the empty string is passed for text, the cell will contain a non-breaking space.
		/// </summary>
		private string text {
			set {
				control = getLiteral( value ?? "" );
				simpleText = value;
			}
		}

		// NOTE: Change the data type to a list of strings.
		/// <summary>
		/// Specifies the CSS class for this cell.
		/// </summary>
		public string CssClass { get { return cssClass; } set { cssClass = value; } }

		/// <summary>
		/// Gets or sets the text alignment.
		/// </summary>
		public TextAlignment TextAlignment { get; set; }

		/// <summary>
		/// EWF ToolTip to display on this control. Setting ToolTipControl will ignore this property. Do not pass null. Setting this to empty string is
		/// equivalent to not setting this at all (unless you are overwriting a previous value, of course).
		/// </summary>
		public string ToolTip { get; set; }

		/// <summary>
		/// Control to display inside the tool tip. This will ignore the ToolTip property. Passing null for this property is equivalent to not setting it at all
		/// (unless you are overwriting a previous value, of course).
		/// </summary>
		public Control ToolTipControl { get; set; }

		// NOTE: Don't allow this to be less than one. Zero is allowed by the HTML spec but is too difficult for us to implement right now.
		/// <summary>
		/// Specifies the number of fields this cell will span.
		/// </summary>
		public int FieldSpan { get { return fieldSpan; } set { fieldSpan = value; } }

		// NOTE: Don't allow this to be less than one. Zero is allowed by the HTML spec but is too difficult for us to implement right now.
		/// <summary>
		/// Specifies the number of items this cell will span.
		/// </summary>
		public int ItemSpan { get { return itemSpan; } set { itemSpan = value; } }

		/// <summary>
		/// Creates an EWF table cell containing the specified text, HTML encoded. If the empty string is passed for text, the cell will contain a non-breaking
		/// space.
		/// </summary>
		public EwfTableCell( string text ) {
			this.text = text;
			TextAlignment = TextAlignment.NotSpecified;
			ToolTip = "";
		}

		// NOTE: Change this constructor to take a parameter array of controls. There are many times when we want to have more than one control in a table cell.
		/// <summary>
		/// Creates an EWF table cell containing the specified control. If null is passed for control, the cell will contain a non-breaking space.
		/// </summary>
		public EwfTableCell( Control control ) {
			Control = control;
			TextAlignment = TextAlignment.NotSpecified;
			ToolTip = "";
		}

		private static Literal getLiteral( string text ) {
			return new Literal { Text = text.GetTextAsEncodedHtml() };
		}

		public static implicit operator EwfTableCell( string text ) {
			return new EwfTableCell( text );
		}

		public static implicit operator EwfTableCell( Control control ) {
			return new EwfTableCell( control );
		}
	}
}