#nullable disable
using System;
using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A style that displays errors in a custom way.
	/// </summary>
	public class CustomErrorDisplayStyle: ErrorDisplayStyle<FlowComponent> {
		private readonly Func<ErrorSourceSet, IEnumerable<TrustedHtmlString>, bool, IReadOnlyCollection<FlowComponent>> componentGetter;

		/// <summary>
		/// Creates a custom error-display style.
		/// </summary>
		/// <param name="componentGetter"></param>
		public CustomErrorDisplayStyle( Func<ErrorSourceSet, IEnumerable<TrustedHtmlString>, bool, IReadOnlyCollection<FlowComponent>> componentGetter ) {
			this.componentGetter = componentGetter;
		}

		IReadOnlyCollection<FlowComponent> ErrorDisplayStyle<FlowComponent>.GetComponents(
			ErrorSourceSet errorSources, IEnumerable<TrustedHtmlString> errors, bool componentsFocusableOnError ) =>
			componentGetter( errorSources, errors, componentsFocusableOnError );
	}
}