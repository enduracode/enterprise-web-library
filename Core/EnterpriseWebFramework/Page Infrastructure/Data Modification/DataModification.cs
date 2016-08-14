using System;
using System.Collections.Generic;
using EnterpriseWebLibrary.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A collection of validation methods and a collection of modification methods.
	/// </summary>
	public interface DataModification: ValidationList {
		[ Obsolete( "Guaranteed through 31 October 2016. Use EwfValidation instead." ) ]
		void AddTopValidationMethod( Action<PostBackValueDictionary, Validator> validationMethod );

		/// <summary>
		/// Adds a modification method. These only execute if all validation methods succeed.
		/// </summary>
		void AddModificationMethod( Action modificationMethod );

		/// <summary>
		/// Adds a list of modification methods. These only execute if all validation methods succeed.
		/// </summary>
		void AddModificationMethods( IEnumerable<Action> modificationMethods );
	}
}