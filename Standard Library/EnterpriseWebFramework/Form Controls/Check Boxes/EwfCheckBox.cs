using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.CssHandling;
using RedStapler.StandardLibrary.JavaScriptWriting;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/* NOTE: This should be named InlineCheckBox. When we do this, rename CommonCheckBox to EwfCheckBox.
	 * 
	 * InlineCheckBox is a check box that can be centered with text-align or used within a paragraph of text. This cannot be done with BlockCheckBox for two reasons:
	 * 
	 * 1. BlockCheckBox needs to have 100% width in order for nested controls to work properly in Chrome and Safari. Specifically, a two-row shrink wrap table with wide content
	 * in the second column of the second row did not work properly in these browsers under certain circumstances (Sam may have details). Inline elements cannot specify width,
	 * so the BlockCheckBox must be non-inline, and non-inline elements cannot be centered with text-align or used within a paragraph of text.
	 * 
	 * 2. We could not find a way to get Chrome and Safari to center a table-based check box control using text-align regardless of what CSS display value we used on the element.
	 * */

	/// <summary>
	/// An in-line check box with the label vertically centered on the box.
	/// </summary>
	public class EwfCheckBox: WebControl, CommonCheckBox, ControlTreeDataLoader, FormControl<bool>, ControlWithCustomFocusLogic {
		internal class CssElementCreator: ControlCssElementCreator {
			internal const string CssClass = "ewfCheckBox";

			CssElement[] ControlCssElementCreator.CreateCssElements() {
				return new[]
					{
						new CssElement( "InlineCheckBox", "label." + CssClass ), new CssElement( "InlineCheckBoxBox", "input." + CssClass ),
						new CssElement( "InlineCheckBoxLabel", "span." + CssClass )
					};
			}
		}

		private readonly bool isCheckedDurable;
		private readonly string label;
		private readonly List<string> onClickJsMethods = new List<string>();
		private CheckBox checkBox;
		private PostBackButton defaultSubmitButton;
		private Func<bool, bool> postBackValueSelector;

		/// <summary>
		/// Creates a check box. Do not pass null for label.
		/// </summary>
		public EwfCheckBox( bool isChecked, string label = "" ) {
			isCheckedDurable = isChecked;
			this.label = label;
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

		/// <summary>
		/// Assigns this to submit the given PostBackButton. This will disable the button's submit behavior. Do not pass null.
		/// </summary>
		public void SetDefaultSubmitButton( PostBackButton pbb ) {
			defaultSubmitButton = pbb;
		}

		internal Func<bool, bool> PostBackValueSelector { set { postBackValueSelector = value; } }

		public bool IsRadioButton { get { return GroupName.Any(); } }

		/// <summary>
		/// Gets whether the box was created in a checked state.
		/// </summary>
		public bool IsChecked { get { return isCheckedDurable; } }

		void ControlTreeDataLoader.LoadData() {
			CssClass = CssElementCreator.CssClass.ConcatenateWithSpace( CssClass );

			checkBox = GroupName.Length > 0 ? new RadioButton { GroupName = GroupName } : new CheckBox();
			checkBox.Checked = AppRequestState.Instance.EwfPageRequestState.PostBackValues.GetValue( this );
			checkBox.AutoPostBack = AutoPostBack;

			// This is an alternative to using the CssClass property, which causes a span to be created around the check box.
			checkBox.InputAttributes.Add( "class", CssElementCreator.CssClass );

			checkBox.AddJavaScriptEventScript( JsWritingMethods.onclick, StringTools.ConcatenateWithDelimiter( "", onClickJsMethods.ToArray() ) );
			if( defaultSubmitButton != null )
				EwfPage.Instance.MakeControlPostBackOnEnter( checkBox, defaultSubmitButton );

			Controls.Add( checkBox );
			EwfLabel labelControl = null;
			if( label.Any() ) {
				labelControl = new EwfLabel { Text = label, CssClass = CssElementCreator.CssClass };
				Controls.Add( labelControl );
			}

			if( ToolTip != null || ToolTipControl != null )
				new ToolTip( ToolTipControl ?? EnterpriseWebFramework.Controls.ToolTip.GetToolTipTextControl( ToolTip ), label.Any() ? labelControl as Control : checkBox );
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
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Label; } }
	}
}