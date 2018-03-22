using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A flow component that can be autofocused.
	/// </summary>
	public class FlowAutofocusRegion: FlowComponent {
		internal readonly AutofocusCondition Condition;
		private readonly IEnumerable<FlowComponentOrNode> children;

		/// <summary>
		/// Creates an autofocus region.
		/// </summary>
		/// <param name="condition">Do not pass null.</param>
		/// <param name="children"></param>
		public FlowAutofocusRegion( AutofocusCondition condition, IEnumerable<FlowComponent> children ) {
			Condition = condition;
			this.children = children;
		}

		IEnumerable<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}