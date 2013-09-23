using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayLinking;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A block-level check box with the label vertically centered on the box.
	/// </summary>
	[ ParseChildren( ChildrenAsProperties = true, DefaultProperty = "NestedControls" ) ]
	public class BlockCheckBox: WebControl, CommonCheckBox, ControlTreeDataLoader, FormControl, ControlWithJsInitLogic, ControlWithCustomFocusLogic {
		private readonly FormValue<bool> checkBoxFormValue;
		private readonly FormValue<CommonCheckBox> radioButtonFormValue;
		private readonly string radioButtonListItemId;
		private readonly string label;
		private readonly bool highlightWhenChecked;
		private readonly List<string> onClickJsMethods = new List<string>();
		private WebControl checkBox;

		/// <summary>
		/// Creates a check box. Do not pass null for label.
		/// </summary>
		public BlockCheckBox( bool isChecked, string label = "", bool highlightWhenChecked = false ) {
			checkBoxFormValue = EwfCheckBox.GetFormValue( isChecked, this );
			this.label = label;
			this.highlightWhenChecked = highlightWhenChecked;
			NestedControls = new List<Control>();
		}

		/// <summary>
		/// Creates a radio button.
		/// </summary>
		internal BlockCheckBox( FormValue<CommonCheckBox> formValue, string label, string listItemId = null ) {
			radioButtonFormValue = formValue;
			radioButtonListItemId = listItemId;
			this.label = label;
			NestedControls = new List<Control>();
		}

		string CommonCheckBox.GroupName { get { return checkBoxFormValue != null ? "" : ( (FormValue)radioButtonFormValue ).GetPostBackValueKey(); } }

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
			onClickJsMethods.Add( jsMethodInvocation );
		}

		public bool IsRadioButton { get { return radioButtonFormValue != null; } }

		/// <summary>
		/// Gets whether the box was created in a checked state.
		/// </summary>
		public bool IsChecked { get { return checkBoxFormValue != null ? checkBoxFormValue.GetDurableValue() : radioButtonFormValue.GetDurableValue() == this; } }

		void ControlTreeDataLoader.LoadData() {
			PreRender += delegate {
				if( highlightWhenChecked && checkBoxFormValue.GetValue( AppRequestState.Instance.EwfPageRequestState.PostBackValues ) )
					CssClass = CssClass.ConcatenateWithSpace( "checkedChecklistCheckboxDiv" );
			};

			var table = TableOps.CreateUnderlyingTable();
			table.CssClass = "ewfBlockCheckBox";

			checkBox = new WebControl( HtmlTextWriterTag.Input );
			PreRender +=
				delegate {
					EwfCheckBox.AddCheckBoxAttributes( checkBox, this, checkBoxFormValue, radioButtonFormValue, radioButtonListItemId, AutoPostBack, onClickJsMethods );
				};

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

		FormValue FormControl.FormValue { get { return (FormValue)checkBoxFormValue ?? radioButtonFormValue; } }

		/// <summary>
		/// Gets whether the box is checked in the post back.
		/// </summary>
		public bool IsCheckedInPostBack( PostBackValueDictionary postBackValues ) {
			return checkBoxFormValue != null ? checkBoxFormValue.GetValue( postBackValues ) : radioButtonFormValue.GetValue( postBackValues ) == this;
		}

		/// <summary>
		/// Returns true if the value changed on this post back.
		/// </summary>
		public bool ValueChangedOnPostBack( PostBackValueDictionary postBackValues ) {
			return checkBoxFormValue != null ? checkBoxFormValue.ValueChangedOnPostBack( postBackValues ) : radioButtonFormValue.ValueChangedOnPostBack( postBackValues );
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