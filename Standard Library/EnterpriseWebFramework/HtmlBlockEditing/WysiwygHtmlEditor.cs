using System;
using System.Collections.Specialized;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.CssHandling;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A WYSIWYG HTML editor.
	/// </summary>
	public class WysiwygHtmlEditor: WebControl, IPostBackDataHandler, ControlTreeDataLoader, ControlWithJsInitLogic, FormControl<string> {
		internal const string CkEditorFolderUrl = "Ewf/CkEditor";

		// Update this if you upgrade to a new version of CKEditor.
		internal static readonly DateTime CkEditorInstallationDate = new DateTime( 2012, 3, 30 );

		private readonly string durableValue;
		private string postValue;

		/// <summary>
		/// Creates a simple HTML editor. Do not pass null for value.
		/// </summary>
		public WysiwygHtmlEditor( string value ) {
			durableValue = value;
		}

		string FormControl<string>.DurableValue { get { return durableValue; } }
		string FormControl.DurableValueAsString { get { return durableValue; } }

		void ControlTreeDataLoader.LoadData( DBConnection cn ) {
			Attributes.Add( "name", UniqueID );

			// The initial NewLine is here because of http://haacked.com/archive/2008/11/18/new-line-quirk-with-html-textarea.aspx and because this is what Microsoft
			// does in their System.Web.UI.WebControls.TextBox implementation. It probably doesn't matter in this case since CKEditor is gutting the textarea, but we
			// want to have this somewhere for reference to assist us when we reimplement EwfTextBox to not use System.Web.UI.WebControls.TextBox under the hood.
			Controls.Add( new Literal
			              	{ Text = HttpUtility.HtmlEncode( Environment.NewLine + AppRequestState.Instance.EwfPageRequestState.PostBackValues.GetValue( this ) ) } );
		}

		string ControlWithJsInitLogic.GetJsInitStatements() {
			return "$( '#" + ClientID + "' ).ckeditor( function() { /* callback code */ }, { toolbar: 'Basic', contentsCss: '" +
			       this.GetClientUrl( "~/" + CkEditorFolderUrl + "/contents" + CssHandler.GetFileVersionString( CkEditorInstallationDate ) + ".css" ) + "' } );";
		}

		bool IPostBackDataHandler.LoadPostData( string postDataKey, NameValueCollection postCollection ) {
			postValue = postCollection[ postDataKey ];
			return false;
		}

		void FormControl.AddPostBackValueToDictionary( PostBackValueDictionary postBackValues ) {
			// This hack prevents the NewLine that CKEditor seems to always add to the end of the textarea from causing ValueChangedOnPostBack to always return true.
			if( postValue.EndsWith( Environment.NewLine ) && postValue.Remove( postValue.Length - Environment.NewLine.Length ) == durableValue )
				postValue = durableValue;

			postBackValues.Add( this, postValue );
		}

		/// <summary>
		/// Gets the post back value.
		/// </summary>
		public string GetPostBackValue( PostBackValueDictionary postBackValues ) {
			return postBackValues.GetValue( this );
		}

		/// <summary>
		/// Do not use.
		/// </summary>
		public string GetPostBackValueOld() {
			return GetPostBackValue( AppRequestState.Instance.EwfPageRequestState.PostBackValues );
		}

		/// <summary>
		/// Returns true if the value changed on this post back.
		/// </summary>
		public bool ValueChangedOnPostBack( PostBackValueDictionary postBackValues ) {
			return postBackValues.ValueChangedOnPostBack( this );
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Textarea; } }

		void IPostBackDataHandler.RaisePostDataChangedEvent() {}
	}
}