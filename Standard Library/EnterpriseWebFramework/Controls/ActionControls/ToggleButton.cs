using System.Collections.Generic;
using System.IO;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayLinking;
using RedStapler.StandardLibrary.JavaScriptWriting;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A control that, when clicked, toggles the display of other controls.
	/// </summary>
	public class ToggleButton: WebControl, ControlTreeDataLoader, ControlWithJsInitLogic, DisplayLink, ActionControl {
		private const string pageStateKey = "controlsToggled";

		private readonly List<Control> controlsToToggle = new List<Control>();

		/// <summary>
		/// Gets or sets the text displayed in the button. Do not set this to null. This is displayed as alternate text for certain action control styles.
		/// NOTE: Do not use.
		/// </summary>
		public string Text { get; set; }

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

		private Unit width = Unit.Empty;
		private Unit height = Unit.Empty;
		private EwfHiddenField controlsToggledHiddenField;
		private Control textControl;

		/// <summary>
		/// Creates a toggle button.
		/// NOTE: Do not use.
		/// </summary>
		public ToggleButton( ActionControlStyle actionControlStyle = null ) {
			Text = "";
			ActionControlStyle = actionControlStyle ?? new ButtonActionControlStyle();
		}

		/// <summary>
		/// Creates a toggle button with ControlsToToggle already populated.
		/// Use SetInitialDisplay on each control to set up the initial visibility of each control.
		/// </summary>
		public ToggleButton( ActionControlStyle actionControlStyle, params WebControl[] controlsToToggle ): this( actionControlStyle ) {
			AddControlsToToggle( controlsToToggle );
		}

		/// <summary>
		/// Creates a toggle button with ControlsToToggle already populated.
		/// Use SetInitialDisplay on each control to set up the initial visibility of each control.
		/// NOTE: Do not use.
		/// </summary>
		public ToggleButton( params WebControl[] controlsToToggle ): this() {
			AddControlsToToggle( controlsToToggle );
		}

		/// <summary>
		/// Creates a toggle button with ControlsToToggle already populated.
		/// Use SetInitialDisplay on each control to set up the initial visibility of each control.
		/// NOTE: Do not use.
		/// </summary>
		public ToggleButton( params HtmlControl[] controlsToToggle ): this() {
			AddControlsToToggle( controlsToToggle );
		}

		/// <summary>
		/// Creates a toggle button with ControlsToToggle already populated.
		/// Use SetInitialDisplay on each control to set up the initial visibility of each control.
		/// </summary>
		public ToggleButton( ActionControlStyle actionControlStyle, params HtmlControl[] controlsToToggle ): this( actionControlStyle ) {
			AddControlsToToggle( controlsToToggle );
		}

		/// <summary>
		/// Does nothing. Overriding this method forces Visual Studio to respect white space around the control when it is used in markup.
		/// </summary>
		protected override void AddParsedSubObject( object obj ) {}

		/// <summary>
		/// Add controls that should be toggled. Use SetInitialDisplay on each control to set up its initial visibility.
		/// </summary>
		public void AddControlsToToggle( params WebControl[] controlsToToggle ) {
			this.controlsToToggle.AddRange( controlsToToggle );
		}

		/// <summary>
		/// Add controls that should be toggled. Use SetInitialDisplay on each control to set up its initial visibility.
		/// </summary>
		public void AddControlsToToggle( params HtmlControl[] controlsToToggle ) {
			this.controlsToToggle.AddRange( controlsToToggle );
		}

		/// <summary>
		/// Gets or sets the CSS classes for this button.
		/// </summary>
		public override string CssClass { get; set; }

		/// <summary>
		/// Gets or sets the width of this button. Doesn't work with the text action control style.
		/// </summary>
		public override Unit Width { get { return width; } set { width = value; } }

		/// <summary>
		/// Gets or sets the height of this button. Only works with the image action control style.
		/// </summary>
		public override Unit Height { get { return height; } set { height = value; } }

		void ControlTreeDataLoader.LoadData( DBConnection cn ) {
			EwfPage.Instance.AddDisplayLink( this );

			AlternateText = AlternateText ?? Text;

			// NOTE: Currently this hidden field will always be persisted in page state whether the page cares about that or not. We should put this decision into the
			// hands of the page, maybe by making ToggleButton sort of like a form control such that it takes a boolean value in its constructor and allows access to
			// its post back value.
			controlsToggledHiddenField = new EwfHiddenField( EwfPage.Instance.PageState.GetValue( this, pageStateKey, false ).ToString() );
			Controls.Add( controlsToggledHiddenField );
			EwfPage.Instance.PostBackDataModification.AddModificationMethod(
				cn1 => AppRequestState.AddNonTransactionalModificationMethod( () => EwfPage.Instance.PageState.SetValue( this, pageStateKey, controlsToggled ) ) );

			var button = new WebControl( PostBackButton.GetTagKey( ActionControlStyle ) );

			// Add the button to the page right away since we use UniqueID below.
			Controls.Add( button );

			if( PostBackButton.GetTagKey( ActionControlStyle ) == HtmlTextWriterTag.Button )
				PostBackButton.AddButtonAttributes( button );
			button.AddJavaScriptEventScript( JsWritingMethods.onclick, handlerName + "()" );
			button.CssClass = CssClass.ConcatenateWithSpace( "ewfClickable" );
			textControl = ActionControlStyle.SetUpControl( button, Text, width, height, w => base.Width = w );

			// If the action control style has configured the button to be a block container, make this control also a block container.
			if( button.CssClass.Separate().Contains( "ewfBlockContainer" ) )
				CssClass = CssClass.ConcatenateWithSpace( "ewfBlockContainer" );
		}

		string ControlWithJsInitLogic.GetJsInitStatements() {
			return ActionControlStyle.GetJsInitStatements( this );
		}

		void DisplayLink.AddJavaScript() {
			using( var sw = new StringWriter() ) {
				sw.WriteLine( "function " + handlerName + "() {" );

				sw.WriteLine( "var controlsToggled = document.getElementById( '" + controlsToggledHiddenField.ClientID + "' );" );
				if( textControl != null )
					sw.WriteLine( "var textElement = document.getElementById( '" + textControl.ClientID + "' );" );

				sw.WriteLine( "if( controlsToggled.value == '" + true + "' ) {" );
				sw.WriteLine( "controlsToggled.value = '" + false + "';" );
				if( textControl != null )
					sw.WriteLine( "textElement.innerHTML = '" + ( ActionControlStyle.Text.Length > 0 ? ActionControlStyle.Text : Text ) + "';" );
				sw.WriteLine( "}" );

				sw.WriteLine( "else {" );
				sw.WriteLine( "controlsToggled.value = '" + true + "';" );
				if( textControl != null ) {
					sw.WriteLine( "textElement.innerHTML = '" + AlternateText + "';" );
					sw.WriteLine( "if( textElement.innerHTML == '' ) { setElementDisplay( '" + ClientID + "', false ); }" );
				}
				sw.WriteLine( "}" );

				foreach( var c in controlsToToggle )
					sw.WriteLine( "toggleElementDisplay( '" + c.ClientID + "' );" );

				sw.WriteLine( "}" );
				EwfPage.Instance.ClientScript.RegisterClientScriptBlock( GetType(), UniqueID, sw.ToString(), true );
			}
		}

		private string handlerName { get { return "toggleState_" + ClientID; } }

		void DisplayLink.SetInitialDisplay() {
			if( controlsToggled ) {
				if( AlternateText.Length > 0 ) {
					textControl.Controls.Clear();
					textControl.Controls.Add( AlternateText.GetLiteralControl() );
				}
				else
					this.SetInitialDisplay( false );
				foreach( var control in controlsToToggle ) {
					if( control is WebControl )
						( control as WebControl ).ToggleInitialDisplay();
					else
						( control as HtmlControl ).ToggleInitialDisplay();
				}
			}
		}

		private bool controlsToggled {
			get {
				bool result;
				return bool.TryParse( controlsToggledHiddenField.GetPostBackValue( AppRequestState.Instance.EwfPageRequestState.PostBackValues ), out result ) && result;
			}
		}

		/// <summary>
		/// Returns the span tag, which represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Span; } }
	}
}