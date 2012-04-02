using System;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.JavaScriptWriting;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
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
		private bool isCheckedDurable;
		private string text;
		private readonly List<string> onClickJsMethods = new List<string>();
		private CheckBox checkBox;
		private readonly List<Action> checkedHandlers = new List<Action>();
		private PostBackButton defaultSubmitButton;

		/// <summary>
		/// Creates a check box. Do not pass null for label.
		/// </summary>
		public EwfCheckBox( bool isChecked, string label = "" ) {
			isCheckedDurable = isChecked;
			text = label;
			GroupName = "";
		}

		/// <summary>
		/// Do not use.
		/// </summary>
		public EwfCheckBox(): this( false ) {}

		/// <summary>
		/// Do not use.
		/// </summary>
		public EwfCheckBox( string text ): this( false, label: text ?? "" ) {}

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
		/// Do not use.
		/// </summary>
		public Action CheckedChangedHandler { private get; set; }

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

		void CommonCheckBox.AddCheckedHandler( Action method ) {
			checkedHandlers.Add( method );
		}

		/// <summary>
		/// Assigns this to submit the given PostBackButton. This will disable the button's submit behavior. Do not pass null.
		/// </summary>
		public void SetDefaultSubmitButton( PostBackButton pbb ) {
			defaultSubmitButton = pbb;
		}

		void ControlTreeDataLoader.LoadData( DBConnection cn ) {
			CssClass = "ewfCheckBox".ConcatenateWithSpace( CssClass );

			checkBox = GroupName.Length > 0 ? new RadioButton { GroupName = GroupName } : new CheckBox();
			checkBox.Checked = AppRequestState.Instance.EwfPageRequestState.PostBackValues.GetValue( this );
			checkBox.AutoPostBack = AutoPostBack;

			checkBox.AddJavaScriptEventScript( JsWritingMethods.onclick, StringTools.ConcatenateWithDelimiter( "", onClickJsMethods.ToArray() ) );
			if( defaultSubmitButton != null )
				EwfPage.Instance.MakeControlPostBackOnEnter( checkBox, defaultSubmitButton );

			Controls.Add( checkBox );
			HtmlGenericControl label = null;
			if( text.Length > 0 ) {
				label = new HtmlGenericControl( "span" ) { InnerText = text };
				Controls.Add( label );
			}

			if( CheckedChangedHandler != null )
				checkBox.CheckedChanged += delegate { CheckedChangedHandler(); };

			foreach( var handler in checkedHandlers ) {
				var handlerCopy = handler;
				checkBox.CheckedChanged += ( sender, e ) => {
					if( ( (CheckBox)sender ).Checked )
						handlerCopy();
				};
			}

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
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Label; } }
	}
}