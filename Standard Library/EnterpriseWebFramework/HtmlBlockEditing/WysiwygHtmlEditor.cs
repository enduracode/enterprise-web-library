using System;
using System.Collections.Specialized;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A WYSIWYG HTML editor.
	/// </summary>
	public class WysiwygHtmlEditor: WebControl, ControlTreeDataLoader, ControlWithJsInitLogic, FormControl {
		private readonly FormValue<string> formValue;
		private readonly string ckEditorVariableOverrides;
		/// <summary>
		/// Creates a simple HTML editor. Do not pass null for value.
		/// To customize the underlying CKEditor (changing the toolbar, etc.) you may pass in a comma-separated list of variable overrides ("toolbar: ['Bold', 'Italic'], etc. ). 'contentsCss: ' will be set automatically.
		/// </summary>
		public WysiwygHtmlEditor( string value, string ckEditorVariableOverrides = null ) {
			this.ckEditorVariableOverrides = ckEditorVariableOverrides;
			formValue = new FormValue<string>(
				() => value,
				() => this.IsOnPage() ? UniqueID : "",
				v => v,
				rawValue => {
					if( rawValue == null )
						return PostBackValueValidationResult<string>.CreateInvalid();

					// This hack prevents the NewLine that CKEditor seems to always add to the end of the textarea from causing
					// ValueChangedOnPostBack to always return true.
					if( rawValue.EndsWith( Environment.NewLine ) && rawValue.Remove( rawValue.Length - Environment.NewLine.Length ) == formValue.GetDurableValue() )
						rawValue = formValue.GetDurableValue();

					return PostBackValueValidationResult<string>.CreateValidWithValue( rawValue );
				} );
		}

		void ControlTreeDataLoader.LoadData() {
			Attributes.Add( "name", UniqueID );
			PreRender += delegate { EwfTextBox.AddTextareaValue( this, formValue.GetValue( AppRequestState.Instance.EwfPageRequestState.PostBackValues ) ); };
		}

		string ControlWithJsInitLogic.GetJsInitStatements() {
			const string toolbar =
				"[ 'Source', '-', 'Bold', 'Italic', '-', 'NumberedList', 'BulletedList', '-', 'JustifyLeft', 'JustifyCenter', 'JustifyRight', 'JustifyBlock', '-', 'Image', 'Table', 'HorizontalRule', '-', 'Link', 'Unlink', 'Styles' ]";

			var variableSpecs = ckEditorVariableOverrides ?? "toolbar: [ " + toolbar + " ]";
			return "CKEDITOR.replace( '" + ClientID + "', { " + variableSpecs + " } );";
		}

		FormValue FormControl.FormValue { get { return formValue; } }

		/// <summary>
		/// Gets the post back value.
		/// </summary>
		public string GetPostBackValue( PostBackValueDictionary postBackValues ) {
			return formValue.GetValue( postBackValues );
		}

		/// <summary>
		/// Returns true if the value changed on this post back.
		/// </summary>
		public bool ValueChangedOnPostBack( PostBackValueDictionary postBackValues ) {
			return formValue.ValueChangedOnPostBack( postBackValues );
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Textarea; } }
	}
}