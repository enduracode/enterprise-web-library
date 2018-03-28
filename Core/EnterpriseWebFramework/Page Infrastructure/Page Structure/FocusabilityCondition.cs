namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A condition that determines whether an element is focusable.
	/// </summary>
	public sealed class FocusabilityCondition {
		internal bool IsNormallyFocusable { get; }
		internal ErrorSourceSet ErrorFocusabilitySources { get; }

		public FocusabilityCondition( bool isNormallyFocusable, ErrorSourceSet errorFocusabilitySources = null ) {
			IsNormallyFocusable = isNormallyFocusable;
			ErrorFocusabilitySources = errorFocusabilitySources ?? new ErrorSourceSet();
		}
	}
}