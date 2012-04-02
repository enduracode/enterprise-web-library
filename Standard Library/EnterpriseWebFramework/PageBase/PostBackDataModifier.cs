using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.Validation;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// NOTE: Do not use this interface. It will be deleted.
	/// </summary>
	public interface PostBackDataModifier {
		/// <summary>
		/// Validate the form values entered by the user.
		/// </summary>
		void ValidateFormValues( Validator validator );

		/// <summary>
		/// Modify the data using the specified database connection string.
		/// </summary>
		void ModifyData( DBConnection cn );
	}
}