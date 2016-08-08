using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The configuration for a WYSIWYG HTML editor.
	/// </summary>
	public class WysiwygHtmlEditorSetup {
		internal readonly DisplaySetup DisplaySetup;
		internal readonly string CkEditorConfiguration;
		internal readonly Func<bool, bool> ValidationPredicate;
		internal readonly Action ValidationErrorNotifier;

		/// <summary>
		/// Creates an HTML editor setup object.
		/// </summary>
		/// <param name="displaySetup"></param>
		/// <param name="ckEditorConfiguration">A comma-separated list of CKEditor configuration options ("toolbar: [ [ 'Bold', 'Italic' ] ]", etc.). Use this to
		/// customize the underlying CKEditor. Do not pass null.</param>
		/// <param name="validationPredicate"></param>
		/// <param name="validationErrorNotifier"></param>
		public WysiwygHtmlEditorSetup(
			DisplaySetup displaySetup = null, string ckEditorConfiguration = "", Func<bool, bool> validationPredicate = null, Action validationErrorNotifier = null ) {
			DisplaySetup = displaySetup;
			CkEditorConfiguration = ckEditorConfiguration;
			ValidationPredicate = validationPredicate;
			ValidationErrorNotifier = validationErrorNotifier;
		}
	}
}