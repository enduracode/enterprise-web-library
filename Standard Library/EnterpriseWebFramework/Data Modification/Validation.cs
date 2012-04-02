using System;
using RedStapler.StandardLibrary.Validation;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A validation.
	/// </summary>
	public class Validation {
		private readonly Action<PostBackValueDictionary, Validator> method;

		/// <summary>
		/// Creates a validation with the specified method and adds it to the specified validation list.
		/// </summary>
		public Validation( Action<PostBackValueDictionary, Validator> method, ValidationList validationList ) {
			this.method = method;

			if( validationList is DataModification )
				( validationList as DataModification ).AddValidation( this );
			else
				( validationList as BasicValidationList ).AddValidation( this );
		}

		internal Action<PostBackValueDictionary, Validator> Method { get { return method; } }
	}
}