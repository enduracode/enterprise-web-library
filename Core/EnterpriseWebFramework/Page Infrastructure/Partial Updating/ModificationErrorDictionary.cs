using System.Collections.Generic;
using System.Collections.Immutable;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public class ModificationErrorDictionary {
		private readonly ImmutableDictionary<EwfValidation, IReadOnlyCollection<string>> errorsByValidation;
		private readonly IReadOnlyCollection<TrustedHtmlString> generalErrors;

		internal ModificationErrorDictionary(
			ImmutableDictionary<EwfValidation, IReadOnlyCollection<string>> errorsByValidation, IReadOnlyCollection<TrustedHtmlString> generalErrors ) {
			this.errorsByValidation = errorsByValidation;
			this.generalErrors = generalErrors;
		}

		/// <summary>
		/// Returns the errors from the specified validation.
		/// </summary>
		public IReadOnlyCollection<string> GetValidationErrors( EwfValidation validation ) => errorsByValidation[ validation ];

		/// <summary>
		/// Returns the general errors.
		/// </summary>
		public IReadOnlyCollection<TrustedHtmlString> GetGeneralErrors() => generalErrors;
	}
}