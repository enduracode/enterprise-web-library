#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using Tewl.InputValidation;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A WYSIWYG HTML editor.
	/// </summary>
	public class WysiwygHtmlEditor: FormControl<FlowComponent> {
		public FlowComponent PageComponent { get; }
		public EwfValidation Validation { get; }

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

			PageComponent = new ElementComponent(
				context => {
					var displaySetup = setup.DisplaySetup ?? new DisplaySetup( true );
					displaySetup.AddJsShowStatements( getJsShowStatements( context.Id, false, setup.CkEditorConfiguration ) );
					displaySetup.AddJsHideStatements( "CKEDITOR.instances.{0}.destroy(); $( '#{0}' ).css( 'display', 'none' );".FormatWith( context.Id ) );

					return new ElementData(
						() => {
							var attributes = new List<ElementAttribute>();
							if( setup.IsReadOnly )
								attributes.Add( new ElementAttribute( "disabled" ) );
							else
								attributes.Add( new ElementAttribute( "name", context.Id ) );
							if( !displaySetup.ComponentsDisplayed )
								attributes.Add( new ElementAttribute( "style", "display: none" ) );

							return new ElementLocalData(
								"textarea",
								new FocusabilityCondition( !setup.IsReadOnly ),
								isFocused => new ElementFocusDependentData(
									attributes: attributes,
									includeIdAttribute: true,
									jsInitStatements: displaySetup.ComponentsDisplayed ? getJsShowStatements( context.Id, isFocused, setup.CkEditorConfiguration ) : "" ) );
						},
						clientSideIdReferences: id.ToCollection(),
						children: new TextNode( () => TextControlSetup.GetTextareaValue( modificationValue.Value ) ).ToCollection() );
				},
				formValue: formValue );

			formValue.AddPageModificationValue( modificationValue, v => v );

			Validation = formValue.CreateValidation(
				( postBackValue, validator ) => {
					if( setup.ValidationPredicate != null && !setup.ValidationPredicate( postBackValue.ChangedOnPostBack ) )
						return;

					var errorHandler = new ValidationErrorHandler( "HTML" );
					var validatedValue = maxLength.HasValue
						                     ? validator.GetString( errorHandler, postBackValue.Value, allowEmpty, maxLength.Value )
						                     : validator.GetString( errorHandler, postBackValue.Value, allowEmpty );
					if( errorHandler.LastResult != ErrorCondition.NoError ) {
						setup.ValidationErrorNotifier?.Invoke();
						return;
					}

					validationMethod( validatedValue, validator );
				} );
		}

		private string getJsShowStatements( string id, bool ckEditorIsFocused, string ckEditorConfiguration ) {
			var startupFocus = "startupFocus: {0}".FormatWith( ckEditorIsFocused ? "true" : "false" );

			const string toolbar =
				"[ 'Source', '-', 'Bold', 'Italic', '-', 'NumberedList', 'BulletedList', '-', 'JustifyLeft', 'JustifyCenter', 'JustifyRight', 'JustifyBlock', '-', 'Image', 'Table', 'HorizontalRule', '-', 'Link', 'Unlink', 'Styles' ]";
			var configuration = ckEditorConfiguration.Any() ? ckEditorConfiguration : "toolbar: [ " + toolbar + " ]";

			return "CKEDITOR.replace( '" + id + "', { " + startupFocus + ", " + configuration + " } );";
		}

		FormControlLabeler FormControl<FlowComponent>.Labeler => null;
	}
}