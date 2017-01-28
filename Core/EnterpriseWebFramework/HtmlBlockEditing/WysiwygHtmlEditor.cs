using System;
using System.Collections.Generic;
using System.Linq;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.InputValidation;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A WYSIWYG HTML editor.
	/// </summary>
	public class WysiwygHtmlEditor: FormControl<FlowComponentOrNode> {
		private readonly FlowComponentOrNode component;
		private readonly EwfValidation validation;

		/// <summary>
		/// Creates a simple HTML editor.
		/// </summary>
		/// <param name="value">Do not pass null.</param>
		/// <param name="allowEmpty"></param>
		/// <param name="validationMethod">The validation method. Do not pass null.</param>
		/// <param name="setup">The setup object for the HTML editor.</param>
		/// <param name="maxLength"></param>
		public WysiwygHtmlEditor(
			string value, bool allowEmpty, Action<string, Validator> validationMethod, WysiwygHtmlEditorSetup setup = null, int? maxLength = null ) {
			setup = setup ?? new WysiwygHtmlEditorSetup();

			var id = new ElementId();
			FormValue<string> formValue = null;
			formValue = new FormValue<string>(
				() => value,
				() => setup.IsReadOnly ? "" : id.Id,
				v => v,
				rawValue => {
					if( rawValue == null )
						return PostBackValueValidationResult<string>.CreateInvalid();

					// This hack prevents the NewLine that CKEditor seems to always add to the end of the textarea from causing
					// ValueChangedOnPostBack to always return true.
					if( rawValue.EndsWith( Environment.NewLine ) && rawValue.Remove( rawValue.Length - Environment.NewLine.Length ) == formValue.GetDurableValue() )
						rawValue = formValue.GetDurableValue();

					return PostBackValueValidationResult<string>.CreateValid( rawValue );
				} );

			var modificationValue = new PageModificationValue<string>();

			component = new PageElement(
				context => {
					id.AddId( context.Id );

					var displaySetup = setup.DisplaySetup ?? new DisplaySetup( true );
					var jsShowStatements = getJsShowStatements( context.Id, setup.CkEditorConfiguration );
					displaySetup.AddJsShowStatements( jsShowStatements );
					displaySetup.AddJsHideStatements( "CKEDITOR.instances.{0}.destroy(); $( '#{0}' ).css( 'display', 'none' );".FormatWith( context.Id ) );

					return new ElementData(
						() => {
							var attributes = new List<Tuple<string, string>>();
							if( setup.IsReadOnly )
								attributes.Add( Tuple.Create( "disabled", "disabled" ) );
							else
								attributes.Add( Tuple.Create( "name", context.Id ) );
							if( !displaySetup.ComponentsDisplayed )
								attributes.Add( Tuple.Create( "style", "display: none" ) );

							return new ElementLocalData( "textarea", attributes, true, displaySetup.ComponentsDisplayed ? jsShowStatements : "" );
						},
						children: new TextNode( () => EwfTextBox.GetTextareaValue( modificationValue.Value ) ).ToCollection() );
				},
				formValue: formValue );

			validation = formValue.CreateValidation(
				( postBackValue, validator ) => {
					if( setup.ValidationPredicate != null && !setup.ValidationPredicate( postBackValue.ChangedOnPostBack ) )
						return;

					var errorHandler = new ValidationErrorHandler( "HTML" );
					var validatedValue = maxLength.HasValue
						                     ? validator.GetString( errorHandler, postBackValue.Value, allowEmpty, maxLength.Value )
						                     : validator.GetString( errorHandler, postBackValue.Value, allowEmpty );
					if( errorHandler.LastResult != ErrorCondition.NoError ) {
						setup.ValidationErrorNotifier();
						return;
					}

					validationMethod( validatedValue, validator );
				} );

			formValue.AddPageModificationValue( modificationValue, v => v );
		}

		private string getJsShowStatements( string id, string ckEditorConfiguration ) {
			const string toolbar =
				"[ 'Source', '-', 'Bold', 'Italic', '-', 'NumberedList', 'BulletedList', '-', 'JustifyLeft', 'JustifyCenter', 'JustifyRight', 'JustifyBlock', '-', 'Image', 'Table', 'HorizontalRule', '-', 'Link', 'Unlink', 'Styles' ]";
			var configuration = ckEditorConfiguration.Any() ? ckEditorConfiguration : "toolbar: [ " + toolbar + " ]";
			return "CKEDITOR.replace( '" + id + "', { " + configuration + " } );";
		}

		public FlowComponentOrNode PageComponent { get { return component; } }
		public EwfValidation Validation { get { return validation; } }
	}
}