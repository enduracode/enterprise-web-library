using System;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A WYSIWYG HTML editor.
	/// </summary>
	public class WysiwygHtmlEditor: WebControl, ControlTreeDataLoader, ControlWithJsInitLogic, FormControl {
		private readonly string ckEditorConfiguration;
		private readonly FormValue<string> formValue;

		/// <summary>
		/// Creates a simple HTML editor.
		/// </summary>
		/// <param name="value">Do not pass null.</param>
		/// <param name="ckEditorConfiguration">A comma-separated list of CKEditor configuration options ("toolbar: [ [ 'Bold', 'Italic' ] ]", etc.). Use this to
		/// customize the underlying CKEditor. Do not pass null.</param>
		public WysiwygHtmlEditor( string value, string ckEditorConfiguration = "" ) {
			this.ckEditorConfiguration = ckEditorConfiguration;

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
			var configuration = ckEditorConfiguration.Any() ? ckEditorConfiguration : "toolbar: [ " + toolbar + " ]";
			return "CKEDITOR.replace( '" + ClientID + "', { " + configuration + " } );";
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