#nullable disable
using System;
using Tewl.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The configuration for an HTML block editor.
	/// </summary>
	public class HtmlBlockEditorSetup {
		internal readonly DisplaySetup DisplaySetup;
		internal readonly WysiwygHtmlEditorSetup WysiwygSetup;
		internal readonly Action<Validator> AdditionalValidationMethod;

		/// <summary>
		/// Creates an HTML block editor setup object.
		/// </summary>
		/// <param name="displaySetup"></param>
		/// <param name="isReadOnly">Pass true to prevent the contents of the HTML block editor from being changed.</param>
		/// <param name="ckEditorConfiguration">A comma-separated list of CKEditor configuration options ("toolbar: [ [ 'Bold', 'Italic' ] ]", etc.). Use this to
		/// customize the underlying CKEditor. Do not pass null.</param>
		/// <param name="validationPredicate"></param>
		/// <param name="validationErrorNotifier"></param>
		/// <param name="additionalValidationMethod"></param>
		public HtmlBlockEditorSetup(
			DisplaySetup displaySetup = null, bool isReadOnly = false, string ckEditorConfiguration = "", Func<bool, bool> validationPredicate = null,
			Action validationErrorNotifier = null, Action<Validator> additionalValidationMethod = null ) {
			DisplaySetup = displaySetup;
			WysiwygSetup = new WysiwygHtmlEditorSetup(
				isReadOnly: isReadOnly,
				ckEditorConfiguration: ckEditorConfiguration,
				validationPredicate: validationPredicate,
				validationErrorNotifier: validationErrorNotifier );
			AdditionalValidationMethod = additionalValidationMethod;
		}
	}
}