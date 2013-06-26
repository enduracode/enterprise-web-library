using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayLinking;
using RedStapler.StandardLibrary.JavaScriptWriting;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A block-level check box with the label vertically centered on the box.
	/// </summary>
	[ ParseChildren( ChildrenAsProperties = true, DefaultProperty = "NestedControls" ) ]
	public class BlockCheckBox: WebControl, CommonCheckBox, ControlTreeDataLoader, FormControl<bool>, ControlWithJsInitLogic, ControlWithCustomFocusLogic {
		private readonly bool isCheckedDurable;
		private readonly string label;
		private readonly bool highlightWhenChecked;
		private readonly List<string> onClickJsMethods = new List<string>();
		private Func<bool, bool> postBackValueSelector;
		private CheckBox checkBox;

		/// <summary>
		/// Creates a check box. Do not pass null for label.
		/// </summary>
		public BlockCheckBox( bool isChecked, string label = "", bool highlightWhenChecked = false ) {
			isCheckedDurable = isChecked;
			this.label = label;
			this.highlightWhenChecked = highlightWhenChecked;
			NestedControls = new List<Control>();
			GroupName = "";
			postBackValueSelector = isCheckedInPostBack => isCheckedInPostBack;
		}

		bool FormControl<bool>.DurableValue { get { return isCheckedDurable; } }
		string FormControl.DurableValueAsString { get { return isCheckedDurable.ToString(); } }

		/// <summary>
		/// Gets or sets the name of the group that this check box belongs to. If this is not the empty string, this control will render as a radio button rather
		/// than a check box.
		/// </summary>
		internal string GroupName { private get; set; }

		string CommonCheckBox.GroupName { get { return GroupName; } }

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

		internal Func<bool, bool> PostBackValueSelector { set { postBackValueSelector = value; } }

		public bool IsRadioButton { get { return GroupName.Any(); } }

		/// <summary>
		/// Gets whether the box was created in a checked state.
		/// </summary>
		public bool IsChecked { get { return isCheckedDurable; } }

		void ControlTreeDataLoader.LoadData() {
			if( highlightWhenChecked && AppRequestState.Instance.EwfPageRequestState.PostBackValues.GetValue( this ) )
				CssClass = CssClass.ConcatenateWithSpace( "checkedChecklistCheckboxDiv" );

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

			var labelControl = new HtmlGenericControl( "label" ) { InnerText = label };
			row.Cells.Add( new TableCell().AddControlsReturnThis( labelControl ) );
			PreRender += ( s, e ) => labelControl.Attributes.Add( "for", checkBox.ClientID );

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
				new ToolTip( ToolTipControl ?? EnterpriseWebFramework.Controls.ToolTip.GetToolTipTextControl( ToolTip ), label.Length > 0 ? (Control)labelControl : checkBox );
		}

		string ControlWithJsInitLogic.GetJsInitStatements() {
			return highlightWhenChecked ? "$( '#" + checkBox.ClientID + "' ).click( function() { changeCheckBoxColor( this ); } );" : "";
		}

		void FormControl.AddPostBackValueToDictionary( PostBackValueDictionary postBackValues ) {
			postBackValues.Add( this, checkBox.Checked );
		}

		/// <summary>
		/// Gets whether the box is checked in the post back.
		/// </summary>
		public bool IsCheckedInPostBack( PostBackValueDictionary postBackValues ) {
			return postBackValueSelector( postBackValues.GetValue( this ) );
		}

		/// <summary>
		/// Returns true if the value changed on this post back.
		/// </summary>
		public bool ValueChangedOnPostBack( PostBackValueDictionary postBackValues ) {
			return IsCheckedInPostBack( postBackValues ) != isCheckedDurable;
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