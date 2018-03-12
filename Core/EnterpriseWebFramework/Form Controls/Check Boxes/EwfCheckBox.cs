using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.JavaScriptWriting;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
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
	public class EwfCheckBox: WebControl, CommonCheckBox, ControlTreeDataLoader, FormValueControl, ControlWithCustomFocusLogic {
		private static readonly ElementClass elementClass = new ElementClass( "ewfCheckBox" );

		internal class CssElementCreator: ControlCssElementCreator {
			IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() {
				return new[]
					{
						new CssElement( "InlineCheckBox", "label." + elementClass.ClassName ), new CssElement( "InlineCheckBoxBox", "input." + elementClass.ClassName ),
						new CssElement( "InlineCheckBoxLabel", "span." + elementClass.ClassName )
					};
			}
		}

		internal static FormValue<bool> GetFormValue( bool isChecked, Control checkBox ) {
			return new FormValue<bool>(
				() => isChecked,
				() => checkBox.IsOnPage() ? checkBox.UniqueID : "",
				v => v.ToString(),
				rawValue => rawValue == null
					            ? PostBackValueValidationResult<bool>.CreateValid( false )
					            : rawValue == "on"
						            ? PostBackValueValidationResult<bool>.CreateValid( true )
						            : PostBackValueValidationResult<bool>.CreateInvalid() );
		}

		internal static void AddCheckBoxAttributes(
			WebControl checkBoxElement, Control checkBox, FormValue<bool> checkBoxFormValue, FormValue<CommonCheckBox> radioButtonFormValue,
			string radioButtonListItemId, FormAction action, bool autoPostBack, IEnumerable<string> onClickJsMethods ) {
			checkBoxElement.Attributes.Add( "type", checkBoxFormValue != null ? "checkbox" : "radio" );
			checkBoxElement.Attributes.Add( "name", checkBoxFormValue != null ? checkBox.UniqueID : ( (FormValue)radioButtonFormValue ).GetPostBackValueKey() );
			if( radioButtonFormValue != null )
				checkBoxElement.Attributes.Add( "value", radioButtonListItemId ?? checkBox.UniqueID );
			if( checkBoxFormValue != null
				    ? checkBoxFormValue.GetValue( AppRequestState.Instance.EwfPageRequestState.PostBackValues )
				    : radioButtonFormValue.GetValue( AppRequestState.Instance.EwfPageRequestState.PostBackValues ) == checkBox )
				checkBoxElement.Attributes.Add( "checked", "checked" );

			var implicitSubmissionStatements = SubmitButton.GetImplicitSubmissionKeyPressStatements( action, false, legacy: true );
			if( implicitSubmissionStatements.Any() )
				checkBoxElement.AddJavaScriptEventScript( JsWritingMethods.onkeypress, implicitSubmissionStatements );

			var isSelectedRadioButton = radioButtonFormValue != null &&
			                            radioButtonFormValue.GetValue( AppRequestState.Instance.EwfPageRequestState.PostBackValues ) == checkBox;
			var postBackScript = autoPostBack && !isSelectedRadioButton ? action.GetJsStatements() : "";
			var customScript = StringTools.ConcatenateWithDelimiter( "; ", onClickJsMethods.ToArray() );
			checkBoxElement.AddJavaScriptEventScript( JsWritingMethods.onclick, StringTools.ConcatenateWithDelimiter( "; ", postBackScript, customScript ) );
		}

		private readonly FormValue<bool> checkBoxFormValue;
		private readonly FormValue<CommonCheckBox> radioButtonFormValue;
		private readonly string radioButtonListItemId;
		private readonly string label;
		private readonly FormAction action;
		private readonly List<Func<IEnumerable<string>>> jsClickHandlerStatementLists = new List<Func<IEnumerable<string>>>();
		private WebControl checkBox;

		/// <summary>
		/// Creates a check box. Do not pass null for label.
		/// </summary>
		public EwfCheckBox( bool isChecked, string label = "", FormAction action = null ) {
			checkBoxFormValue = GetFormValue( isChecked, this );
			this.label = label;
			this.action = action ?? FormState.Current.DefaultAction;
		}

		/// <summary>
		/// Creates a radio button.
		/// </summary>
		internal EwfCheckBox(
			FormValue<CommonCheckBox> formValue, string label, FormAction action, Func<IEnumerable<string>> jsClickHandlerStatementListGetter,
			string listItemId = null ) {
			radioButtonFormValue = formValue;
			radioButtonListItemId = listItemId;
			this.label = label;
			this.action = action ?? FormState.Current.DefaultAction;
			jsClickHandlerStatementLists.Add( jsClickHandlerStatementListGetter );
		}

		string CommonCheckBox.GroupName => checkBoxFormValue != null ? "" : ( (FormValue)radioButtonFormValue ).GetPostBackValueKey();

		/// <summary>
		/// Gets or sets whether or not the check box automatically posts the page back to the server when it is checked or unchecked.
		/// </summary>
		public bool AutoPostBack { get; set; }

		/// <summary>
		/// Adds a javascript method to be called when the check box is clicked.  Example: AddOnClickJsMethod( "changeCheckBoxColor( this )" ).
		/// </summary>
		public void AddOnClickJsMethod( string jsMethodInvocation ) {
			jsClickHandlerStatementLists.Add( jsMethodInvocation.ToCollection );
		}

		public bool IsRadioButton => radioButtonFormValue != null;

		/// <summary>
		/// Gets whether the box was created in a checked state.
		/// </summary>
		public bool IsChecked => checkBoxFormValue != null ? checkBoxFormValue.GetDurableValue() : radioButtonFormValue.GetDurableValue() == this;

		void ControlTreeDataLoader.LoadData() {
			action.AddToPageIfNecessary();

			CssClass = elementClass.ClassName.ConcatenateWithSpace( CssClass );

			checkBox = new WebControl( HtmlTextWriterTag.Input );
			PreRender += delegate {
				AddCheckBoxAttributes(
					checkBox,
					this,
					checkBoxFormValue,
					radioButtonFormValue,
					radioButtonListItemId,
					action,
					AutoPostBack,
					jsClickHandlerStatementLists.SelectMany( i => i() ) );
				checkBox.Attributes.Add( "class", elementClass.ClassName );
			};
			Controls.Add( checkBox );

			if( label.Any() )
				this.AddControlsReturnThis( new GenericPhrasingContainer( label.ToComponents(), classes: elementClass ).ToCollection().GetControls() );
		}

		FormValue FormValueControl.FormValue => (FormValue)checkBoxFormValue ?? radioButtonFormValue;

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
			return checkBoxFormValue != null
				       ? checkBoxFormValue.ValueChangedOnPostBack( postBackValues )
				       : radioButtonFormValue.ValueChangedOnPostBack( postBackValues );
		}

		void ControlWithCustomFocusLogic.SetFocus() {
			Page.SetFocus( checkBox );
		}

		/// <summary>
		/// Returns the div tag, which represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey => HtmlTextWriterTag.Label;
	}
}