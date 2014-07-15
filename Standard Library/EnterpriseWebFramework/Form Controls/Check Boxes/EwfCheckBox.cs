using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
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
	public class EwfCheckBox: WebControl, CommonCheckBox, ControlTreeDataLoader, FormControl, ControlWithCustomFocusLogic {
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

		internal static FormValue<bool> GetFormValue( bool isChecked, Control checkBox ) {
			return new FormValue<bool>( () => isChecked,
				() => checkBox.IsOnPage() ? checkBox.UniqueID : "",
				v => v.ToString(),
				rawValue =>
				rawValue == null
					? PostBackValueValidationResult<bool>.CreateValidWithValue( false )
					: rawValue == "on" ? PostBackValueValidationResult<bool>.CreateValidWithValue( true ) : PostBackValueValidationResult<bool>.CreateInvalid() );
		}

		internal static void AddCheckBoxAttributes( WebControl checkBoxElement, Control checkBox, FormValue<bool> checkBoxFormValue,
		                                            FormValue<CommonCheckBox> radioButtonFormValue, string radioButtonListItemId, PostBack postBack, bool autoPostBack,
		                                            IEnumerable<string> onClickJsMethods ) {
			checkBoxElement.Attributes.Add( "type", checkBoxFormValue != null ? "checkbox" : "radio" );
			checkBoxElement.Attributes.Add( "name", checkBoxFormValue != null ? checkBox.UniqueID : ( (FormValue)radioButtonFormValue ).GetPostBackValueKey() );
			if( radioButtonFormValue != null )
				checkBoxElement.Attributes.Add( "value", radioButtonListItemId ?? checkBox.UniqueID );
			if( checkBoxFormValue != null
				    ? checkBoxFormValue.GetValue( AppRequestState.Instance.EwfPageRequestState.PostBackValues )
				    : radioButtonFormValue.GetValue( AppRequestState.Instance.EwfPageRequestState.PostBackValues ) == checkBox )
				checkBoxElement.Attributes.Add( "checked", "checked" );

			PostBackButton.EnsureImplicitSubmission( checkBoxElement, postBack );
			var isSelectedRadioButton = radioButtonFormValue != null &&
			                            radioButtonFormValue.GetValue( AppRequestState.Instance.EwfPageRequestState.PostBackValues ) == checkBox;
			var postBackScript = autoPostBack && !isSelectedRadioButton
				                     ? PostBackButton.GetPostBackScript( postBack ?? EwfPage.Instance.DataUpdatePostBack, includeReturnFalse: false )
				                     : "";
			var customScript = StringTools.ConcatenateWithDelimiter( "; ", onClickJsMethods.ToArray() );
			checkBoxElement.AddJavaScriptEventScript( JsWritingMethods.onclick, StringTools.ConcatenateWithDelimiter( "; ", postBackScript, customScript ) );
		}

		private readonly FormValue<bool> checkBoxFormValue;
		private readonly FormValue<CommonCheckBox> radioButtonFormValue;
		private readonly string radioButtonListItemId;
		private readonly string label;
		private readonly PostBack postBack;
		private readonly List<string> onClickJsMethods = new List<string>();
		private WebControl checkBox;

		/// <summary>
		/// Creates a check box. Do not pass null for label.
		/// </summary>
		public EwfCheckBox( bool isChecked, string label = "", PostBack postBack = null ) {
			checkBoxFormValue = GetFormValue( isChecked, this );
			this.label = label;
			this.postBack = postBack;
		}

		/// <summary>
		/// Creates a radio button.
		/// </summary>
		internal EwfCheckBox( FormValue<CommonCheckBox> formValue, string label, PostBack postBack, string listItemId = null ) {
			radioButtonFormValue = formValue;
			radioButtonListItemId = listItemId;
			this.label = label;
			this.postBack = postBack;
		}

		string CommonCheckBox.GroupName { get { return checkBoxFormValue != null ? "" : ( (FormValue)radioButtonFormValue ).GetPostBackValueKey(); } }

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
			onClickJsMethods.Add( jsMethodInvocation );
		}

		public bool IsRadioButton { get { return radioButtonFormValue != null; } }

		/// <summary>
		/// Gets whether the box was created in a checked state.
		/// </summary>
		public bool IsChecked { get { return checkBoxFormValue != null ? checkBoxFormValue.GetDurableValue() : radioButtonFormValue.GetDurableValue() == this; } }

		void ControlTreeDataLoader.LoadData() {
			if( postBack != null || AutoPostBack )
				EwfPage.Instance.AddPostBack( postBack ?? EwfPage.Instance.DataUpdatePostBack );

			CssClass = CssElementCreator.CssClass.ConcatenateWithSpace( CssClass );

			checkBox = new WebControl( HtmlTextWriterTag.Input );
			PreRender += delegate {
				AddCheckBoxAttributes( checkBox, this, checkBoxFormValue, radioButtonFormValue, radioButtonListItemId, postBack, AutoPostBack, onClickJsMethods );
				checkBox.Attributes.Add( "class", CssElementCreator.CssClass );
			};
			Controls.Add( checkBox );

			EwfLabel labelControl = null;
			if( label.Any() ) {
				labelControl = new EwfLabel { Text = label, CssClass = CssElementCreator.CssClass };
				Controls.Add( labelControl );
			}

			if( ToolTip != null || ToolTipControl != null )
				new ToolTip( ToolTipControl ?? EnterpriseWebFramework.Controls.ToolTip.GetToolTipTextControl( ToolTip ), label.Any() ? labelControl : checkBox );
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
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Label; } }
	}
}