using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	[ Obsolete( "Guaranteed through 31 October 2016. Use ValidationSetupState.ExecuteWithDataModifications instead." ) ]
	public interface ValidationList {
		/// <summary>
		/// Adds all validations from the specified basic validation list.
		/// </summary>
		void AddValidations( BasicValidationList validationList );
	}

	internal interface ValidationListInternal {
		void AddValidation( EwfValidation validation );
	}
}