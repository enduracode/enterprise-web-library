using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayLinking;
using RedStapler.StandardLibrary.JavaScriptWriting;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A block-level check box with the label vertically centered on the box.
	/// </summary>
	[ ParseChildren( ChildrenAsProperties = true, DefaultProperty = "NestedControls" ) ]
	public class BlockCheckBox: WebControl, CommonCheckBox, ControlTreeDataLoader, FormControl<bool>, ControlWithCustomFocusLogic {
		private bool isCheckedDurable;
		private string text;
		private readonly List<string> onClickJsMethods = new List<string>();
		private CheckBox checkBox;

		/// <summary>
		/// Creates a check box. Do not pass null for label.
		/// </summary>
		public BlockCheckBox( bool isChecked, string label = "" ) {
			isCheckedDurable = isChecked;
			text = label;
			NestedControls = new List<Control>();
			GroupName = "";
		}

		/// <summary>
		/// Do not use.
		/// </summary>
		public BlockCheckBox(): this( false ) {}

		/// <summary>
		/// Do not use.
		/// </summary>
		public BlockCheckBox( string text ): this( false, label: text ?? "" ) {}

		bool FormControl<bool>.DurableValue { get { return isCheckedDurable; } }
		string FormControl.DurableValueAsString { get { return isCheckedDurable.ToString(); } }

		/// <summary>
		/// Do not use.
		/// </summary>
		public string Text { get { return text; } set { text = value ?? ""; } }

		/// <summary>
		/// Do not use.
		/// </summary>
		public bool Checked { get { return IsCheckedInPostBack( AppRequestState.Instance.EwfPageRequestState.PostBackValues ); } set { isCheckedDurable = value; } }

		/// <summary>
		/// Gets or sets the name of the group that this check box belongs to. If this is not the empty string, this control will render as a radio button rather
		/// than a check box.
		/// </summary>
		public string GroupName { get; set; }

		/// <summary>
		/// Gets or sets whether or not the check box automatically posts the page back to the server when it is checked or unchecked.
		/// </summary>
		public bool AutoPostBack { get; set; }

		/// <summary>
		/// Sets a control that appears beneath the check box's label only when the box is checked.
		/// Controls added to this collection do not need to be added to the page separately.
		/// NOTE: We should make this an Add method instead or exposing the collection.
		/// </summary>
		public List<Control> NestedControls { get; private set; }

		/// <summary>
		/// Sets whether or not the nested controls, if any exist, are always visible or only visible when the box is checked.
		/// </summary>
		public bool NestedControlsAlwaysVisible { private get; set; }

		/// <summary>
		/// EWF ToolTip to display on this control. Setting ToolTipControl will ignore this property.
		/// </summary>
		public override string ToolTip { get; set; }

		/// <summary>
		/// Control to display inside the tool tip. Do not pass null. This will ignore the ToolTip property.
		/// </summary>
		public Control ToolTipControl { get; set; }

		/// <summary>
		/// Adds a javascript method to be called when the check box is clicked.  Example: AddOnClickJsMethod( "changeCheckBoxColor( this )" ).
		/// </summary>
		public void AddOnClickJsMethod( string jsMethodInvocation ) {
			// This method is smart because it will be called both before and after the actual check box or radio button is created.
			// DisplayLinking calls this after LoadData.
			if( checkBox != null )
				checkBox.AddJavaScriptEventScript( JsWritingMethods.onclick, jsMethodInvocation );
			else
				onClickJsMethods.Add( jsMethodInvocation );
		}

		void ControlTreeDataLoader.LoadData( DBConnection cn ) {
			var table = TableOps.CreateUnderlyingTable();
			table.CssClass = "ewfBlockCheckBox";

			checkBox = GroupName.Length > 0 ? new RadioButton { GroupName = GroupName } : new CheckBox();
			checkBox.Checked = AppRequestState.Instance.EwfPageRequestState.PostBackValues.GetValue( this );
			checkBox.AutoPostBack = AutoPostBack;

			checkBox.AddJavaScriptEventScript( JsWritingMethods.onclick, StringTools.ConcatenateWithDelimiter( "", onClickJsMethods.ToArray() ) );

			var checkBoxCell = new TableCell().AddControlsReturnThis( checkBox );
			checkBoxCell.Style.Add( "width", "13px" );

			var row = new TableRow();
			row.Cells.Add( checkBoxCell );

			var label = new HtmlGenericControl( "label" ) { InnerText = text };
			row.Cells.Add( new TableCell().AddControlsReturnThis( label ) );
			PreRender += ( s, e ) => label.Attributes.Add( "for", checkBox.ClientID );

			table.Rows.Add( row );

			if( NestedControls.Any() ) {
				var nestedControlRow = new TableRow();
				nestedControlRow.Cells.Add( new TableCell() );
				nestedControlRow.Cells.Add( new TableCell().AddControlsReturnThis( NestedControls ) );
				table.Rows.Add( nestedControlRow );

				if( !NestedControlsAlwaysVisible )
					CheckBoxToControlArrayDisplayLink.AddToPage( this, true, nestedControlRow );
			}

			Controls.Add( table );
			if( ToolTip != null || ToolTipControl != null )
				new ToolTip( ToolTipControl ?? EnterpriseWebFramework.Controls.ToolTip.GetToolTipTextControl( ToolTip ), text.Length > 0 ? (Control)label : checkBox );
		}

		void FormControl.AddPostBackValueToDictionary( PostBackValueDictionary postBackValues ) {
			postBackValues.Add( this, checkBox.Checked );
		}

		/// <summary>
		/// Gets whether the box is checked in the post back.
		/// </summary>
		public bool IsCheckedInPostBack( PostBackValueDictionary postBackValues ) {
			return postBackValues.GetValue( this );
		}

		/// <summary>
		/// Returns true if the value changed on this post back.
		/// </summary>
		public bool ValueChangedOnPostBack( PostBackValueDictionary postBackValues ) {
			return postBackValues.ValueChangedOnPostBack( this );
		}

		void ControlWithCustomFocusLogic.SetFocus() {
			Page.SetFocus( checkBox );
		}

		/// <summary>
		/// Returns the div tag, which represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Div; } }
	}
}