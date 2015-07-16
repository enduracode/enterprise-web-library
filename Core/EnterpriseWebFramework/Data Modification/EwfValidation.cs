using System;
using EnterpriseWebLibrary.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A validation.
	/// </summary>
	// We renamed this from just Validation because it conflicted with the Validation namespace in the System.Collections.Immutable package.
	public class EwfValidation {
		private readonly Action<PostBackValueDictionary, Validator> method;

		/// <summary>
		/// Creates a validation with the specified method and adds it to the specified validation list.
		/// </summary>
		/// <param name="method">The method that will be called by the data modification(s) to which this validation is added. Within the method, do not add
		/// modification methods to outside lists; this adds confusion and commonly leads to modification methods being skipped or executing in the wrong order.
		/// </param>
		/// <param name="validationList">The DataModification or BasicValidationList to which this validation will be added.</param>
		public EwfValidation( Action<PostBackValueDictionary, Validator> method, ValidationList validationList ) {
			this.method = method;
			( (ValidationListInternal)validationList ).AddValidation( this );
		}

		internal Action<PostBackValueDictionary, Validator> Method { get { return method; } }
	}
}