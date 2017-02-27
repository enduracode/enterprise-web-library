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
	public class BlockCheckBox: WebControl, CommonCheckBox, ControlTreeDataLoader, FormValueControl, ControlWithJsInitLogic, ControlWithCustomFocusLogic,
		FormControl<FlowComponent> {
		private readonly FormValue<bool> checkBoxFormValue;
		private readonly FormValue<CommonCheckBox> radioButtonFormValue;
		private readonly string radioButtonListItemId;
		private readonly string label;
		private readonly BlockCheckBoxSetup setup;
		private readonly FormAction action;
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
		/// <param name="setup">The setup object for the check box.</param>
		public BlockCheckBox( bool isChecked, Action<PostBackValue<bool>, Validator> validationMethod, string label = "", BlockCheckBoxSetup setup = null ) {
			this.setup = setup ?? new BlockCheckBoxSetup();

			checkBoxFormValue = EwfCheckBox.GetFormValue( isChecked, this );

			this.label = label;
			action = this.setup.Action ?? FormState.Current.DefaultAction;

			validation = checkBoxFormValue.CreateValidation( validationMethod );

			nestedControls = this.setup.NestedControlListGetter != null ? this.setup.NestedControlListGetter().ToImmutableArray() : ImmutableArray<Control>.Empty;
		}

		/// <summary>
		/// Creates a radio button.
		/// </summary>
		internal BlockCheckBox(
			FormValue<CommonCheckBox> formValue, string label, BlockCheckBoxSetup setup, Func<IEnumerable<string>> jsClickHandlerStatementListGetter,
			EwfValidation validation, string listItemId = null ) {
			radioButtonFormValue = formValue;
			radioButtonListItemId = listItemId;
			this.label = label;
			this.setup = setup;
			action = setup.Action ?? FormState.Current.DefaultAction;
			jsClickHandlerStatementLists.Add( jsClickHandlerStatementListGetter );

			this.validation = validation;

			nestedControls = setup.NestedControlListGetter != null ? setup.NestedControlListGetter().ToImmutableArray() : ImmutableArray<Control>.Empty;
		}

		FlowComponent FormControl<FlowComponent>.PageComponent { get { throw new ApplicationException( "not implemented" ); } }

		string CommonCheckBox.GroupName { get { return checkBoxFormValue != null ? "" : ( (FormValue)radioButtonFormValue ).GetPostBackValueKey(); } }

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
			action.AddToPageIfNecessary();

			PreRender += delegate {
				if( setup.HighlightedWhenChecked && checkBoxFormValue.GetValue( AppRequestState.Instance.EwfPageRequestState.PostBackValues ) )
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
						action,
						setup.TriggersPostBackWhenCheckedOrUnchecked,
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

				if( !setup.NestedControlsAlwaysVisible )
					CheckBoxToControlArrayDisplayLink.AddToPage( this, true, nestedControlRow );
			}

			Controls.Add( table );
			if( ToolTip != null || ToolTipControl != null )
				new Controls.ToolTip(
					ToolTipControl ?? EnterpriseWebFramework.Controls.ToolTip.GetToolTipTextControl( ToolTip ),
					label.Length > 0 ? (Control)labelControl : checkBox );
		}

		string ControlWithJsInitLogic.GetJsInitStatements() {
			return setup.HighlightedWhenChecked ? "$( '#" + checkBox.ClientID + "' ).click( function() { changeCheckBoxColor( this ); } );" : "";
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