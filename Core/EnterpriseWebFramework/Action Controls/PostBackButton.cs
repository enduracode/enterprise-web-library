using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.JavaScriptWriting;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A control that, when clicked, causes a post back and executes code.
	/// </summary>
	public class PostBackButton: WebControl, ControlTreeDataLoader, ControlWithJsInitLogic, ActionControl {
		internal static HtmlTextWriterTag GetTagKey( ActionControlStyle actionControlStyle ) {
			// NOTE: In theory, we should always return the button tag, but buttons are difficult to style in IE7.
			// NOTE: Another problem with button is that according to the HTML Standard, it can only contain phrasing content.
			return actionControlStyle is TextActionControlStyle || actionControlStyle is CustomActionControlStyle ? HtmlTextWriterTag.A : HtmlTextWriterTag.Button;
		}

		internal static void AddButtonAttributes( WebControl control ) {
			control.Attributes.Add( "name", EwfPage.ButtonElementName );
			control.Attributes.Add( "value", "v" );
			control.Attributes.Add( "type", "button" );
		}

		private bool usesSubmitBehavior;
		private ModalWindow confirmationWindow;
		private readonly PostBackFormAction postBackAction;
		private readonly IReadOnlyCollection<DataModification> dataModifications;

		/// <summary>
		/// Gets or sets the display style of this button. Do not set this to null.
		/// Choices are: ButtonActionControlStyle (default), BoxActionControlStyle, ImageActionControlStyle, CustomActionControlStyle, and TextActionControlStyle.
		/// </summary>
		public ActionControlStyle ActionControlStyle { get; set; }

		[ Obsolete( "Guaranteed through 31 January 2014. Please specify via constructor." ) ]
		public bool UsesSubmitBehavior { get { return usesSubmitBehavior; } set { usesSubmitBehavior = value; } }

		/// <summary>
		/// Setting the content control will cause clicking the button to display a confirmation window with the given control as content displayed to the user.
		/// When this is set, this PostBackButton may not use submit behavior. This may not be used to display more than a small amount of content (e.g. some text 
		/// and a link), and absolutely may not be used with form controls.
		/// </summary>
		public Control ConfirmationWindowContentControl { get; set; }

		/// <summary>
		/// Creates a post-back button.
		/// </summary>
		/// <param name="actionControlStyle"></param>
		/// <param name="usesSubmitBehavior">True if this button should act like a submit button (respond to the enter key). Doesn't work with the text or custom
		/// action control styles.</param>
		/// <param name="postBack">Pass null to use the post-back corresponding to the first of the current data modifications.</param>
		public PostBackButton( ActionControlStyle actionControlStyle, bool usesSubmitBehavior = true, PostBack postBack = null ) {
			ActionControlStyle = actionControlStyle;
			this.usesSubmitBehavior = usesSubmitBehavior;
			postBackAction = new PostBackFormAction( postBack ?? FormState.Current.PostBack );

			EwfPage.Instance.AddControlTreeValidation(
				() => {
					if( !this.IsOnPage() || !this.usesSubmitBehavior )
						return;
					var submitButtons = EwfPage.Instance.GetDescendants( EwfPage.Instance ).OfType<PostBackButton>().Where( i => i.usesSubmitBehavior ).ToArray();
					if( submitButtons.Length > 1 )
						throw new ApplicationException(
							"Multiple buttons with submit behavior were detected. There may only be one per page. The button IDs are " +
							StringTools.ConcatenateWithDelimiter( ", ", submitButtons.Select( control => control.UniqueID ).ToArray() ) + "." );
					EwfPage.Instance.SubmitButtonPostBack = postBackAction.PostBack;
				} );

			dataModifications = FormState.Current.DataModifications;
		}

		[ Obsolete( "Guaranteed through 31 October 2016. Use the constructor in which the post-back is optional." ) ]
		public PostBackButton( PostBack postBack, ActionControlStyle actionControlStyle, bool usesSubmitBehavior = true )
			: this( actionControlStyle, usesSubmitBehavior: usesSubmitBehavior, postBack: postBack ) {}

		/// <summary>
		/// Creates a post-back button.
		/// </summary>
		/// <param name="postBack">Pass null to use the post-back corresponding to the first of the current data modifications.</param>
		// This constructor is needed because of ActionButtonSetups, which take the text in the ActionButtonSetup instead of here and the submit behavior will be overridden.
		public PostBackButton( PostBack postBack = null ): this( new ButtonActionControlStyle( "" ), postBack: postBack ) {}

		void ControlTreeDataLoader.LoadData() {
			FormState.ExecuteWithDataModificationsAndDefaultAction(
				dataModifications,
				() => {
					if( TagKey == HtmlTextWriterTag.Button ) {
						Attributes.Add( "name", EwfPage.ButtonElementName );
						Attributes.Add( "value", "v" );
						Attributes.Add( "type", usesSubmitBehavior ? "submit" : "button" );
					}

					FormAction action = postBackAction;
					action.AddToPageIfNecessary();

					if( ConfirmationWindowContentControl != null ) {
						if( usesSubmitBehavior )
							throw new ApplicationException( "PostBackButton cannot be the submit button and also have a confirmation message." );
						confirmationWindow = new ModalWindow( this, ConfirmationWindowContentControl, title: "Confirmation", postBack: postBackAction.PostBack );
					}
					else if( !usesSubmitBehavior )
						PreRender += delegate { this.AddJavaScriptEventScript( JsWritingMethods.onclick, action.GetJsStatements() + " return false" ); };

					CssClass = CssClass.ConcatenateWithSpace( "ewfClickable" );
					ActionControlStyle.SetUpControl( this, "" );
				} );
		}

		string ControlWithJsInitLogic.GetJsInitStatements() {
			if( ConfirmationWindowContentControl != null )
				this.AddJavaScriptEventScript( JsWritingMethods.onclick, confirmationWindow.GetJsOpenStatement() + " return false" );
			return ActionControlStyle.GetJsInitStatements();
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey => usesSubmitBehavior ? HtmlTextWriterTag.Button : GetTagKey( ActionControlStyle );
	}
}