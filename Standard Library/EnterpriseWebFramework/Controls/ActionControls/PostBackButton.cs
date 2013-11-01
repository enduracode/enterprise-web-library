using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.JavaScriptWriting;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A control that, when clicked, causes a post back and executes code.
	/// </summary>
	public class PostBackButton: WebControl, ControlTreeDataLoader, IPostBackEventHandler, ControlWithJsInitLogic, ActionControl {
		internal static string GetPostBackScript( Control targetControl, bool isEventPostBack, bool includeReturnFalse = true ) {
			if( !( targetControl is IPostBackEventHandler ) && isEventPostBack )
				throw new ApplicationException( "The target must be a post back event handler." );
			var pbo = new PostBackOptions( targetControl, isEventPostBack ? EwfPage.EventPostBackArgument : "" );
			return EwfPage.Instance.ClientScript.GetPostBackEventReference( pbo ) + ( includeReturnFalse ? "; return false" : "" );
		}

		internal static HtmlTextWriterTag GetTagKey( ActionControlStyle actionControlStyle ) {
			// NOTE: In theory, we should always return the button tag, but buttons are difficult to style in IE7.
			// NOTE: Another problem with button is that according to the HTML Standard, it can only contain phrasing content.
			return actionControlStyle is TextActionControlStyle || actionControlStyle is CustomActionControlStyle ? HtmlTextWriterTag.A : HtmlTextWriterTag.Button;
		}

		internal static void AddButtonAttributes( WebControl control ) {
			control.Attributes.Add( "name", control.UniqueID );
			control.Attributes.Add( "value", "v" );
			control.Attributes.Add( "type", "button" );
		}

		private readonly DataModification dataModification;
		private Unit width = Unit.Empty;
		private Unit height = Unit.Empty;
		private ModalWindow confirmationWindow;

		/// <summary>
		/// Gets or sets the display style of this button. Do not set this to null.
		/// Choices are: ButtonActionControlStyle (default), BoxActionControlStyle, ImageActionControlStyle, CustomActionControlStyle, and TextActionControlStyle.
		/// </summary>
		public ActionControlStyle ActionControlStyle { get; set; }

		/// <summary>
		/// True if this button should act like a submit button (respond to the enter key). Doesn't work with the text or custom action control styles.
		/// </summary>
		public bool UsesSubmitBehavior { get; set; }

		/// <summary>
		/// Setting the content control will cause clicking the button to display a confirmation window with the given control as content displayed to the user.
		/// When this is set, this PostBackButton may not use submit behavior. This may not be used to display more than a small amount of content (e.g. some text 
		/// and a link), and absolutely may not be used with form controls.
		/// </summary>
		public Control ConfirmationWindowContentControl { get; set; }

		/// <summary>
		/// Creates a post back button. You may pass null for the clickHandler.
		/// </summary>
		public PostBackButton( DataModification dataModification, Action clickHandler, ActionControlStyle actionControlStyle, bool usesSubmitBehavior = true ) {
			if( dataModification == EwfPage.Instance.DataUpdate )
				throw new ApplicationException( "The page's data update should only be executed by the framework." );
			this.dataModification = dataModification;

			ClickHandler = clickHandler;
			ActionControlStyle = actionControlStyle;
			UsesSubmitBehavior = usesSubmitBehavior;
		}

		/// <summary>
		/// Creates a post back button.
		/// </summary>
		public PostBackButton( DataModification dataModification, ActionControlStyle actionControlStyle, bool usesSubmitBehavior = true )
			: this( dataModification, null, actionControlStyle, usesSubmitBehavior ) {}

		/// <summary>
		/// Creates a post back button. Do not pass null for the data modification.
		/// </summary>
		// This constructor is needed because of ActionButtonSetups, which take the text in the ActionButtonSetup instead of here and the submit behavior will be overridden.
		public PostBackButton( DataModification dataModification, Action clickHandler ): this( dataModification, clickHandler, new ButtonActionControlStyle( "" ) ) {}

		/// <summary>
		/// Sets the method to be invoked when this button is clicked.
		/// </summary>
		public Action ClickHandler { private get; set; }

		/// <summary>
		/// Gets or sets the width of this button. Doesn't work with the text action control style.
		/// </summary>
		public override Unit Width { get { return width; } set { width = value; } }

		/// <summary>
		/// Gets or sets the height of this button. Only works with the image action control style.
		/// </summary>
		public override Unit Height { get { return height; } set { height = value; } }

		void ControlTreeDataLoader.LoadData() {
			if( TagKey == HtmlTextWriterTag.Button ) {
				Attributes.Add( "name", UniqueID );
				Attributes.Add( "value", "v" );
				Attributes.Add( "type", UsesSubmitBehavior ? "submit" : "button" );
			}

			if( ConfirmationWindowContentControl != null ) {
				if( UsesSubmitBehavior )
					throw new ApplicationException( "PostBackButton cannot be the submit button and also have a confirmation message." );
				confirmationWindow = new ModalWindow( ConfirmationWindowContentControl, title: "Confirmation", postBackButton: this );
			}
			else if( !UsesSubmitBehavior )
				this.AddJavaScriptEventScript( JsWritingMethods.onclick, GetPostBackScript( this, true ) );

			CssClass = CssClass.ConcatenateWithSpace( "ewfClickable" );
			ActionControlStyle.SetUpControl( this, "", width, height, setWidth );
		}

		private void setWidth( Unit w ) {
			base.Width = w;
		}

		string ControlWithJsInitLogic.GetJsInitStatements() {
			if( ConfirmationWindowContentControl != null ) {
				this.AddJavaScriptEventScript( JsWritingMethods.onclick,
				                               "$( '#" + ( confirmationWindow as EtherealControl ).Control.ClientID + "' ).dialog( 'open' ); return false" );
			}
			return ActionControlStyle.GetJsInitStatements( this );
		}

		void IPostBackEventHandler.RaisePostBackEvent( string eventArgument ) {
			EwfPage.Instance.ExecuteDataModification( dataModification, ClickHandler );
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return UsesSubmitBehavior ? HtmlTextWriterTag.Button : GetTagKey( ActionControlStyle ); } }
	}
}