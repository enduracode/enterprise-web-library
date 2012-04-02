using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.Validation;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayElements.Page {
	/// <summary>
	/// NOTE: Do not use this interface. It will be deleted.
	/// </summary>
	public interface DataModifierWithRightButton {
		/// <summary>
		/// Text shown on the right button.
		/// </summary>
		string RightButtonText { get; }

		/// <summary>
		/// Validate the form values entered by the user.
		/// </summary>
		void ValidateFormValues( Validator validator );

		/// <summary>
		/// Modify the data using the given database connection.  Redirects to the returned URL.
		/// The returned URL may be null to not redirect.
		/// </summary>
		string ModifyData( DBConnection cn );
	}
}