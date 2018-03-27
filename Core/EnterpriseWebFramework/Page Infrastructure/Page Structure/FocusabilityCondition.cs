using System.Collections.Generic;
using System.Collections.Immutable;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A condition that determines whether an element is focusable.
	/// </summary>
	public sealed class FocusabilityCondition {
		internal bool IsNormallyFocusable { get; }
		internal IReadOnlyCollection<EwfValidation> ErrorFocusabilityValidations { get; }
		internal bool IsFocusableOnTopModificationError { get; }

		public FocusabilityCondition(
			bool isNormallyFocusable, IEnumerable<EwfValidation> errorFocusabilityValidations = null, bool isFocusableOnTopModificationError = false ) {
			IsNormallyFocusable = isNormallyFocusable;
			ErrorFocusabilityValidations = errorFocusabilityValidations?.ToImmutableArray() ?? ImmutableArray<EwfValidation>.Empty;
			IsFocusableOnTopModificationError = isFocusableOnTopModificationError;
		}
	}
}