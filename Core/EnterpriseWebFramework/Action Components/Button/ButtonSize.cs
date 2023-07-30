#nullable disable
namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A size for a standard-style button.
	/// </summary>
	public enum ButtonSize {
		/// <summary>
		/// A button that is shrink-wrapped to be almost no larger than an anchor tag.
		/// </summary>
		ShrinkWrap,

		/// <summary>
		/// A typical-sized button.
		/// </summary>
		Normal,

		/// <summary>
		/// A very large button that dominates the screen.
		/// </summary>
		Large
	}

	internal static class ButtonSizeStatics {
		internal static ElementClassSet Class( ButtonSize size ) {
			switch( size ) {
				case ButtonSize.ShrinkWrap:
					return ActionComponentCssElementCreator.ShrinkWrapButtonStyleClass;
				case ButtonSize.Large:
					return ActionComponentCssElementCreator.LargeButtonStyleClass;
				default:
					return ActionComponentCssElementCreator.NormalButtonStyleClass;
			}
		}
	}
}