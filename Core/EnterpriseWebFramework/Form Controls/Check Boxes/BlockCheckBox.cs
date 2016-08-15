using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.EnterpriseWebFramework.DisplayLinking;
using EnterpriseWebLibrary.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A block-level check box with the label vertically centered on the box.
	/// </summary>
	[ ParseChildren( ChildrenAsProperties = true, DefaultProperty = "NestedControls" ) ]
	public class BlockCheckBox: WebControl, CommonCheckBox, ControlTreeDataLoader, FormValueControl, ControlWithJsInitLogic, ControlWithCustomFocusLogic {
		private readonly FormValue<bool> checkBoxFormValue;
		private readonly FormValue<CommonCheckBox> radioButtonFormValue;
		private readonly string radioButtonListItemId;
		private readonly string label;
		private readonly bool highlightWhenChecked;
		private readonly PostBack postBack;
		private readonly List<Func<IEnumerable<string>>> jsClickHandlerStatementLists = new List<Func<IEnumerable<string>>>();
		private readonly EwfValidation validation;
		private readonly IReadOnlyCollection<Control> nestedControls;
		private WebControl checkBox;

		/// <summary>
		/// Creates a check box.
		/// </summary>
		/// <param name="isChecked"></param>
		/// <param name="validationMethod">The validation method. Do not pass null.</param>
		/// <param name="label">Do not pass null.</param>
		/// <param name="highlightWhenChecked"></param>
		/// <param name="postBack"></param>
		/// <param name="nestedControlListGetter">A function that gets the controls that will appear beneath the check box's label only when the box is checked.</param>
		public BlockCheckBox(
			bool isChecked, Action<PostBackValue<bool>, Validator> validationMethod, string label = "", bool highlightWhenChecked = false, PostBack postBack = null,
			Func<IEnumerable<Control>> nestedControlListGetter = null ) {
			checkBoxFormValue = EwfCheckBox.GetFormValue( isChecked, this );

			this.label = label;
			this.highlightWhenChecked = highlightWhenChecked;
			this.postBack = postBack ?? EwfPage.PostBack;

			validation = checkBoxFormValue.CreateValidation( validationMethod );

			nestedControls = nestedControlListGetter != null ? nestedControlListGetter().ToImmutableArray() : ImmutableArray<Control>.Empty;
		}

		/// <summary>
		/// Creates a radio button.
		/// </summary>
		internal BlockCheckBox(
			FormValue<CommonCheckBox> formValue, string label, PostBack postBack, Func<IEnumerable<string>> jsClickHandlerStatementListGetter, EwfValidation validation,
			Func<IEnumerable<Control>> nestedControlListGetter, string listItemId = null ) {
			radioButtonFormValue = formValue;
			radioButtonListItemId = listItemId;
			this.label = label;
			this.postBack = postBack ?? EwfPage.PostBack;
			jsClickHandlerStatementLists.Add( jsClickHandlerStatementListGetter );

			this.validation = validation;

			nestedControls = nestedControlListGetter != null ? nestedControlListGetter().ToImmutableArray() : ImmutableArray<Control>.Empty;
		}

		string CommonCheckBox.GroupName { get { return checkBoxFormValue != null ? "" : ( (FormValue)radioButtonFormValue ).GetPostBackValueKey(); } }

		/// <summary>
		/// Gets or sets whether or not the check box automatically posts the page back to the server when it is checked or unchecked.
		/// </summary>
		public bool AutoPostBack { get; set; }

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
			jsClickHandlerStatementLists.Add( jsMethodInvocation.ToSingleElementArray );
		}

		public EwfValidation Validation { get { return validation; } }

		public bool IsRadioButton { get { return radioButtonFormValue != null; } }

		/// <summary>
		/// Gets whether the box was created in a checked state.
		/// </summary>
		public bool IsChecked { get { return checkBoxFormValue != null ? checkBoxFormValue.GetDurableValue() : radioButtonFormValue.GetDurableValue() == this; } }

		void ControlTreeDataLoader.LoadData() {
			EwfPage.Instance.AddPostBack( postBack );

			PreRender += delegate {
				if( highlightWhenChecked && checkBoxFormValue.GetValue( AppRequestState.Instance.EwfPageRequestState.PostBackValues ) )
					CssClass = CssClass.ConcatenateWithSpace( "checkedChecklistCheckboxDiv" );
			};

			var table = TableOps.CreateUnderlyingTable();
			table.CssClass = "ewfBlockCheckBox";

			checkBox = new WebControl( HtmlTextWriterTag.Input );
			PreRender +=
				delegate {
					EwfCheckBox.AddCheckBoxAttributes(
						checkBox,
						this,
						checkBoxFormValue,
						radioButtonFormValue,
						radioButtonListItemId,
						postBack,
						AutoPostBack,
						jsClickHandlerStatementLists.SelectMany( i => i() ) );
				};

			var checkBoxCell = new TableCell().AddControlsReturnThis( checkBox );
			checkBoxCell.Style.Add( "width", "13px" );

			var row = new TableRow();
			row.Cells.Add( checkBoxCell );

			var labelControl = new HtmlGenericControl( "label" ) { InnerText = label };
			row.Cells.Add( new TableCell().AddControlsReturnThis( labelControl ) );
			PreRender += ( s, e ) => labelControl.Attributes.Add( "for", checkBox.ClientID );

			table.Rows.Add( row );

			if( nestedControls.Any() ) {
				var nestedControlRow = new TableRow();
				nestedControlRow.Cells.Add( new TableCell() );
				nestedControlRow.Cells.Add( new TableCell().AddControlsReturnThis( nestedControls ) );
				table.Rows.Add( nestedControlRow );

				if( !NestedControlsAlwaysVisible )
					CheckBoxToControlArrayDisplayLink.AddToPage( this, true, nestedControlRow );
			}

			Controls.Add( table );
			if( ToolTip != null || ToolTipControl != null )
				new ToolTip(
					ToolTipControl ?? EnterpriseWebFramework.Controls.ToolTip.GetToolTipTextControl( ToolTip ),
					label.Length > 0 ? (Control)labelControl : checkBox );
		}

		string ControlWithJsInitLogic.GetJsInitStatements() {
			return highlightWhenChecked ? "$( '#" + checkBox.ClientID + "' ).click( function() { changeCheckBoxColor( this ); } );" : "";
		}

		FormValue FormValueControl.FormValue { get { return (FormValue)checkBoxFormValue ?? radioButtonFormValue; } }

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