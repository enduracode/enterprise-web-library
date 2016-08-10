using System;
using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	[ Obsolete( "Guaranteed through 31 October 2016. Use ValidationSetupState.ExecuteWithDataModifications instead." ) ]
	public class BasicValidationList: ValidationList, ValidationListInternal {
		private readonly List<EwfValidation> validations = new List<EwfValidation>();

		void ValidationListInternal.AddValidation( EwfValidation validation ) {
			validations.Add( validation );
		}

		/// <summary>
		/// Adds all validations from the specified list.
		/// </summary>
		public void AddValidations( BasicValidationList validationList ) {
			validations.AddRange( validationList.validations );
		}

		internal IEnumerable<EwfValidation> Validations { get { return validations; } }
	}
}