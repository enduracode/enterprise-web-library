#nullable disable
using System.Collections.Generic;
using System.Collections.Immutable;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A set of data-modification error sources.
	/// </summary>
	public sealed class ErrorSourceSet {
		internal IReadOnlyCollection<EwfValidation> Validations { get; }
		internal bool IncludeGeneralErrors { get; }

		/// <summary>
		/// Creates an error-source set.
		/// </summary>
		public ErrorSourceSet( IEnumerable<EwfValidation> validations = null, bool includeGeneralErrors = false ) {
			Validations = validations?.ToImmutableArray() ?? ImmutableArray<EwfValidation>.Empty;
			IncludeGeneralErrors = includeGeneralErrors;
		}
	}
}