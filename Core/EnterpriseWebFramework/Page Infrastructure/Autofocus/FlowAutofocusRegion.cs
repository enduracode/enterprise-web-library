#nullable disable
using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A flow component that can be autofocused.
	/// </summary>
	public class FlowAutofocusRegion: FlowComponent {
		internal readonly AutofocusCondition Condition;
		private readonly IReadOnlyCollection<FlowComponentOrNode> children;

		/// <summary>
		/// Creates an autofocus region.
		/// </summary>
		/// <param name="condition">Do not pass null.</param>
		/// <param name="children"></param>
		public FlowAutofocusRegion( AutofocusCondition condition, IReadOnlyCollection<FlowComponent> children ) {
			Condition = condition;
			this.children = children;
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}