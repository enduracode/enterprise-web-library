using System;
using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A style that displays errors in a custom way.
	/// </summary>
	public class CustomErrorDisplayStyle: ErrorDisplayStyle<FlowComponent> {
		private readonly Func<IEnumerable<string>, IReadOnlyCollection<FlowComponent>> componentGetter;

		/// <summary>
		/// Creates a custom error-display style.
		/// </summary>
		/// <param name="componentGetter"></param>
		public CustomErrorDisplayStyle( Func<IEnumerable<string>, IReadOnlyCollection<FlowComponent>> componentGetter ) {
			this.componentGetter = componentGetter;
		}

		IReadOnlyCollection<FlowComponent> ErrorDisplayStyle<FlowComponent>.GetComponents( IEnumerable<string> errors ) {
			return componentGetter( errors );
		}
	}
}