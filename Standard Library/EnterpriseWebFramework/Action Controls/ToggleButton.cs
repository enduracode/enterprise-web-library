using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayLinking;
using RedStapler.StandardLibrary.JavaScriptWriting;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A control that, when clicked, toggles the display of other controls.
	/// </summary>
	public class ToggleButton: WebControl, ControlTreeDataLoader, ControlWithJsInitLogic, DisplayLink, ActionControl {
		private const string pageStateKey = "controlsToggled";

		private readonly List<WebControl> controlsToToggle = new List<WebControl>();

		/// <summary>
		/// Gets or sets the text to show when this link has been clicked an odd number of times. Pass NULL for this if you want the text to stay the same or the
		/// empty string if you want this link to disappear after the first click. NOTE: Maybe we should reverse these.
		/// NOTE: Rename this. "Alternate text" has special meaning in HTML.
		/// </summary>
		public string AlternateText { get; set; }

		/// <summary>
		/// Gets or sets the display style of this button. Do not set this to null.
		/// Choices are: ButtonActionControlStyle (default) BoxActionControlStyle, CustomActionControlStyle, and TextActionControlStyle.
		/// CustomActionControlStyle and ImageActionControlStyle currently do not work with this control.
		/// </summary>
		public ActionControlStyle ActionControlStyle { get; set; }

		private readonly IEnumerable<string> toggleClasses;
		private Unit width = Unit.Empty;
		private Unit height = Unit.Empty;
		private Func<PostBackValueDictionary, string> controlsToggledHiddenFieldValueGetter;
		private Func<string> controlsToggledHiddenFieldClientIdGetter;
		private Control textControl;

		/// <summary>
		/// Creates a toggle button with ControlsToToggle already populated.
		/// Use SetInitialDisplay on each control to set up the initial visibility of each control.
		/// </summary>
		public ToggleButton( IEnumerable<WebControl> controlsToToggle, ActionControlStyle actionControlStyle, IEnumerable<string> toggleClasses = null ) {
			AddControlsToToggle( controlsToToggle.ToArray() );
			ActionControlStyle = actionControlStyle;
			this.toggleClasses = toggleClasses;
		}

		/// <summary>
		/// Add controls that should be toggled. Use SetInitialDisplay on each control to set up its initial visibility.
		/// </summary>
		public void AddControlsToToggle( params WebControl[] controlsToToggle ) {
			this.controlsToToggle.AddRange( controlsToToggle );
		}

		/// <summary>
		/// Gets or sets the width of this button. Doesn't work with the text action control style.
		/// </summary>
		public override Unit Width { get { return width; } set { width = value; } }

		/// <summary>
		/// Gets or sets the height of this button. Only works with the image action control style.
		/// </summary>
		public override Unit Height { get { return height; } set { height = value; } }

		void ControlTreeDataLoader.LoadData() {
			EwfPage.Instance.AddDisplayLink( this );

			// NOTE: Currently this hidden field will always be persisted in page state whether the page cares about that or not. We should put this decision into the
			// hands of the page, maybe by making ToggleButton sort of like a form control such that it takes a boolean value in its constructor and allows access to
			// its post back value.
			var controlsToggled = false;
			EwfHiddenField.Create(
				this,
				EwfPage.Instance.PageState.GetValue( this, pageStateKey, false ).ToString(),
				postBackValue => controlsToggled = getControlsToggled( postBackValue ),
				EwfPage.Instance.DataUpdate,
				out controlsToggledHiddenFieldValueGetter,
				out controlsToggledHiddenFieldClientIdGetter );
			EwfPage.Instance.DataUpdate.AddModificationMethod(
				() => AppRequestState.AddNonTransactionalModificationMethod( () => EwfPage.Instance.PageState.SetValue( this, pageStateKey, controlsToggled ) ) );

			if( TagKey == HtmlTextWriterTag.Button )
				PostBackButton.AddButtonAttributes( this );
			this.AddJavaScriptEventScript( JsWritingMethods.onclick, handlerName + "()" );
			CssClass = CssClass.ConcatenateWithSpace( "ewfClickable" );
			textControl = ActionControlStyle.SetUpControl( this, "", width, height, w => base.Width = w );
		}

		string ControlWithJsInitLogic.GetJsInitStatements() {
			return ActionControlStyle.GetJsInitStatements( this );
		}

		void DisplayLink.AddJavaScript() {
			using( var sw = new StringWriter() ) {
				sw.WriteLine( "function " + handlerName + "() {" );

				sw.WriteLine( "var controlsToggled = document.getElementById( '" + controlsToggledHiddenFieldClientIdGetter() + "' );" );
				if( textControl != null )
					sw.WriteLine( "var textElement = document.getElementById( '" + textControl.ClientID + "' );" );

				sw.WriteLine( "if( controlsToggled.value == '" + true + "' ) {" );
				sw.WriteLine( "controlsToggled.value = '" + false + "';" );
				if( textControl != null )
					sw.WriteLine( "textElement.innerHTML = '" + ActionControlStyle.Text + "';" );
				sw.WriteLine( "}" );

				sw.WriteLine( "else {" );
				sw.WriteLine( "controlsToggled.value = '" + true + "';" );
				if( textControl != null ) {
					sw.WriteLine( "textElement.innerHTML = '" + getAlternateText() + "';" );
					sw.WriteLine( "if( textElement.innerHTML == '' ) { setElementDisplay( '" + ClientID + "', false ); }" );
				}
				sw.WriteLine( "}" );

				foreach( var c in controlsToToggle ) {
					if( toggleClasses != null )
						sw.WriteLine( "$( '#" + c.ClientID + "' ).toggleClass( '" + StringTools.ConcatenateWithDelimiter( " ", toggleClasses.ToArray() ) + "', 200 );" );
					else
						sw.WriteLine( "toggleElementDisplay( '" + c.ClientID + "' );" );
				}

				sw.WriteLine( "}" );
				EwfPage.Instance.ClientScript.RegisterClientScriptBlock( GetType(), UniqueID, sw.ToString(), true );
			}
		}

		private string handlerName { get { return "toggleState_" + ClientID; } }

		void DisplayLink.SetInitialDisplay( PostBackValueDictionary formControlValues ) {
			if( getControlsToggled( controlsToggledHiddenFieldValueGetter( formControlValues ) ) ) {
				if( textControl != null ) {
					if( getAlternateText().Any() ) {
						textControl.Controls.Clear();
						textControl.Controls.Add( getAlternateText().GetLiteralControl() );
					}
					else
						this.SetInitialDisplay( false );
				}
				foreach( var webControl in controlsToToggle ) {
					if( toggleClasses != null ) {
						foreach( var i in toggleClasses )
							webControl.CssClass = webControl.CssClass.Contains( i ) ? webControl.CssClass.Replace( i, "" ) : webControl.CssClass.ConcatenateWithSpace( i );
					}
					else
						webControl.ToggleInitialDisplay();
				}
			}
		}

		private bool getControlsToggled( string hiddenFieldValue ) {
			bool result;
			return bool.TryParse( hiddenFieldValue, out result ) && result;
		}

		private string getAlternateText() {
			return AlternateText ?? ActionControlStyle.Text;
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return PostBackButton.GetTagKey( ActionControlStyle ); } }
	}
}